using System.Collections.Generic;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Sales;
using Omnichannel.Domain.Entities;

namespace Omnichannel.Application.Interfaces.Sales
{
    public interface IPaymentService : IPaymentGatewayService
    {
        // 1. VietQR: Sinh mã QR chuyển khoản động chuẩn Napas 247 Compact2
       //TRỪU TƯỢNG
       // Cách 1: Khi khách đặt hàng online trên Web -> Truyền cả Đơn hàng vào
        VietQrPaymentDto GenerateVietQr(Order order); // Cách 1: Khi khách đặt hàng online trên Web -> Truyền cả Đơn hàng vào
       // Cách 2: Khi Thu ngân tại quầy POS bán nhanh -> Chỉ cần truyền Mã đơn và Số tiền
        VietQrPaymentDto GenerateVietQr(string orderCode, decimal amount, string? note = null);
        Task<(bool Success, string OrderCode, decimal Amount, string TransactionId, string Message)> ProcessVietQrWebhookAsync(string transactionId, string content, decimal amount, string? signature = null);
        bool VerifyVietQrWebhookSignature(string rawBody, string signature);

        // 2. VNPay: Cổng thanh toán Sandbox kiểm thử
        string CreateVnPayPaymentUrl(Order order, string ipAddress, string? returnUrl = null);
        Task<(bool Success, string OrderCode, decimal Amount, string TransactionNo, string Message)> ProcessVnPayCallbackAsync(IDictionary<string, string> queryData);
        bool ValidateVnPaySignature(IDictionary<string, string> queryData);

        // 3. MoMo: Ví điện tử Sandbox
        Task<(bool Success, string PayUrl, string? QrCodeUrl, string Message)> CreateMoMoPaymentAsync(Order order, string? returnUrl = null, string? ipnUrl = null);
        Task<(bool Success, string OrderCode, decimal Amount, string TransactionId, string Message)> ProcessMoMoCallbackAsync(IDictionary<string, string> queryData);
        bool ValidateMoMoSignature(IDictionary<string, string> queryData);

        // 4. COD: Xác nhận thanh toán khi nhận hàng
        Task<(bool Success, string Message)> ProcessCodOrderAsync(Order order);
    }
}
