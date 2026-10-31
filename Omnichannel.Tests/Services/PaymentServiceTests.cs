using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Services;
using Omnichannel.Tests.Helpers;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class PaymentServiceTests
    {
        private readonly IConfiguration _config;

        public PaymentServiceTests()
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "VietQrConfig:BankId", "MB" },
                { "VietQrConfig:AccountNo", "0909999888" },
                { "VietQrConfig:AccountName", "CONG TY TNHH OMNICHANNEL BEAUTY" },
                { "VietQrConfig:WebhookSecret", "SECRET_KEY_123" },
                { "VnPayConfig:TmnCode", "TESTVNPAY" },
                { "VnPayConfig:HashSecret", "TESTHASHSECRET1234567890ABCDEFGH" },
                { "VnPayConfig:BaseUrl", "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html" },
                { "MoMoConfig:PartnerCode", "MOMOTEST" },
                { "MoMoConfig:SecretKey", "MOMOSECRETKEY1234" }
            };

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        [Fact]
        public void GenerateVietQr_ValidOrder_GeneratesCompact2UrlWithOrderCodeMemo()
        {
            // Arrange
            using var httpClient = new HttpClient();
            var service = new PaymentService(_config, httpClient, NullLogger<PaymentService>.Instance);

            var order = new Order
            {
                OrderCode = "ORD-20261029-ABC123",
                TotalAmount = 750000m
            };

            // Act
            var result = service.GenerateVietQr(order);

            // Assert
            result.Should().NotBeNull();
            result.BankAccountNo.Should().Be("0909999888");
            result.Amount.Should().Be(750000m);
            result.OrderInfo.Should().Be("ORDER_ORD-20261029-ABC123");
            result.QrImageUrl.Should().Contain("img.vietqr.io/image/MB-0909999888-compact2.png");
            result.QrImageUrl.Should().Contain("amount=750000");
            result.QrImageUrl.Should().Contain("ORDER_ORD-20261029-ABC123");
            result.ExpireSeconds.Should().Be(900);
        }

        [Fact]
        public async Task ProcessVietQrWebhookAsync_ValidOrderContent_ReturnsSuccessAndOrderCode()
        {
            // Arrange
            using var httpClient = new HttpClient();
            var service = new PaymentService(_config, httpClient, NullLogger<PaymentService>.Instance);

            string transId = "FT26302991";
            string content = "Thanh toan ORDER_ORD-20261029-XYZ999";
            decimal amount = 1200000m;

            // Act
            var (success, orderCode, processedAmount, transactionId, message) =
                await service.ProcessVietQrWebhookAsync(transId, content, amount);

            // Assert
            success.Should().BeTrue();
            orderCode.Should().Be("ORD-20261029-XYZ999");
            processedAmount.Should().Be(1200000m);
            transactionId.Should().Be(transId);
            message.Should().Contain("thành công");
        }

        [Fact]
        public void CreateVnPayPaymentUrl_ValidOrder_GeneratesVnPayUrlWithHmacSha512SecureHash()
        {
            // Arrange
            using var httpClient = new HttpClient();
            var service = new PaymentService(_config, httpClient, NullLogger<PaymentService>.Instance);

            var order = new Order
            {
                OrderCode = "ORD-VNPAY-01",
                TotalAmount = 500000m
            };

            // Act
            var url = service.CreateVnPayPaymentUrl(order, "127.0.0.1", "http://localhost:5116/Checkout/VnPayCallback");

            // Assert
            url.Should().NotBeNullOrEmpty();
            url.Should().StartWith("https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?");
            url.Should().Contain("vnp_Amount=50000000"); // 500,000 * 100
            url.Should().Contain("vnp_TxnRef=ORD-VNPAY-01");
            url.Should().Contain("vnp_SecureHash=");
        }

        [Fact]
        public async Task ProcessVnPayCallbackAsync_ValidData_ReturnsSuccess()
        {
            // Arrange
            using var httpClient = new HttpClient();
            var service = new PaymentService(_config, httpClient, NullLogger<PaymentService>.Instance);

            var queryData = new Dictionary<string, string>
            {
                { "vnp_Amount", "100000000" }, // 1,000,000 * 100
                { "vnp_TxnRef", "ORD-VNP-SUCCESS-01" },
                { "vnp_TransactionNo", "14889922" },
                { "vnp_ResponseCode", "00" }
            };

            // Act
            var (success, orderCode, amount, transactionNo, message) = await service.ProcessVnPayCallbackAsync(queryData);

            // Assert
            success.Should().BeTrue();
            orderCode.Should().Be("ORD-VNP-SUCCESS-01");
            amount.Should().Be(1000000m);
            transactionNo.Should().Be("14889922");
        }

        [Fact]
        public async Task IdempotencyService_DuplicateCall_PreventsDuplicateProcessing()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new IdempotencyService(context, NullLogger<IdempotencyService>.Instance);

            string key = "VNPAY_ORD-IDEM-01_TRANS9999";

            // Act 1: Lần đầu tiên
            bool before = await service.HasBeenProcessedAsync(key);
            var record = await service.RecordSuccessAsync(key, "/Checkout/VnPayIpn", "hash123", 200, "{\"RspCode\":\"00\"}");

            // Act 2: Lần thứ hai
            bool after = await service.HasBeenProcessedAsync(key);
            var duplicateRecord = await service.RecordSuccessAsync(key, "/Checkout/VnPayIpn", "hash123", 200, "{\"RspCode\":\"00\"}");

            // Assert
            before.Should().BeFalse();
            after.Should().BeTrue();
            record.Id.Should().Be(duplicateRecord.Id);
            record.IdempotencyKey.Should().Be(key);
        }
    }
}
