using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Omnichannel.Application.DTOs.Inventory;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Services;
using Omnichannel.Tests.Helpers;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class StockTransferServiceTests
    {
        private async Task<(Store source, Store dest, User user, Product product, ProductBatch batch)> SetupEntitiesAsync(Omnichannel.Infrastructure.Data.ApplicationDbContext context)
        {
            var source = new Store { Name = "Kho Tổng Hà Nội", Code = "HQ-001", Address = "Hà Nội", IsActive = true };
            var dest = new Store { Name = "Showroom Cầu Giấy", Code = "SH-CG", Address = "Cầu Giấy", IsActive = true };
            var user = new User { Username = "wh_staff", Email = "wh@test.com", FullName = "Nhân Viên Kho" };
            var product = new Product { Name = "Serum HA B5", Sku = "SRM-B5", Slug = "serum-b5", Barcode = "893000000001", Unit = "Chai", IsActive = true };
            
            context.Stores.AddRange(source, dest);
            context.Users.Add(user);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var batch = new ProductBatch
            {
                ProductId = product.Id,
                BatchNumber = "LOT-B5-01",
                ExpDate = DateTime.UtcNow.AddMonths(18),
                InitialQuantity = 100,
                CurrentQuantity = 100,
                Status = "ACTIVE"
            };
            context.ProductBatches.Add(batch);
            await context.SaveChangesAsync();

            context.StoreInventories.Add(new StoreInventory
            {
                StoreId = source.Id,
                ProductBatchId = batch.Id,
                PhysicalQuantity = 100,
                ReservedQuantity = 0
            });
            await context.SaveChangesAsync();

            return (source, dest, user, product, batch);
        }

        [Fact]
        public async Task CreateTransferRequest_ValidData_SetsStatusToPending()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var (source, dest, user, product, _) = await SetupEntitiesAsync(context);
            var service = new StockTransferService(context, NullLogger<StockTransferService>.Instance);

            var dto = new CreateStockTransferDto
            {
                SourceStoreId = source.Id,
                DestinationStoreId = dest.Id,
                Note = "Chuyển 20 chai serum về Showroom Cầu Giấy",
                Items = new List<CreateStockTransferItemDto>
                {
                    new CreateStockTransferItemDto { ProductId = product.Id, RequestedQuantity = 20 }
                }
            };

            // Act
            var result = await service.CreateTransferRequestAsync(dto, user.Id);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be("PENDING");
            result.TotalRequestedQuantity.Should().Be(20);
            result.SourceStoreName.Should().Be("Kho Tổng Hà Nội");
            result.DestinationStoreName.Should().Be("Showroom Cầu Giấy");
            result.TransferCode.Should().StartWith("TRF-");
        }

        [Fact]
        public async Task CreateTransferRequest_SameSourceAndDest_ThrowsException()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var (source, _, user, product, _) = await SetupEntitiesAsync(context);
            var service = new StockTransferService(context, NullLogger<StockTransferService>.Instance);

            var dto = new CreateStockTransferDto
            {
                SourceStoreId = source.Id,
                DestinationStoreId = source.Id, // Trùng nhau
                Items = new List<CreateStockTransferItemDto>
                {
                    new CreateStockTransferItemDto { ProductId = product.Id, RequestedQuantity = 10 }
                }
            };

            // Act
            Func<Task> act = async () => await service.CreateTransferRequestAsync(dto, user.Id);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Kho nguồn và kho đích không được trùng nhau*");
        }

        [Fact]
        public async Task CancelTransfer_PendingStatus_Success()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var (source, dest, user, product, _) = await SetupEntitiesAsync(context);
            var service = new StockTransferService(context, NullLogger<StockTransferService>.Instance);

            var transfer = await service.CreateTransferRequestAsync(new CreateStockTransferDto
            {
                SourceStoreId = source.Id,
                DestinationStoreId = dest.Id,
                Items = new List<CreateStockTransferItemDto>
                {
                    new CreateStockTransferItemDto { ProductId = product.Id, RequestedQuantity = 10 }
                }
            }, user.Id);

            // Act
            var isCancelled = await service.CancelTransferAsync(transfer.Id, user.Id, "Đã hủy do chi nhánh còn đủ hàng");

            // Assert
            isCancelled.Should().BeTrue();
            var updated = await service.GetTransferByIdAsync(transfer.Id);
            updated!.Status.Should().Be("CANCELLED");
            updated.CancellationReason.Should().Be("Đã hủy do chi nhánh còn đủ hàng");
        }

        [Fact]
        public async Task CancelTransfer_NotPending_ThrowsException()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var (source, dest, user, product, _) = await SetupEntitiesAsync(context);
            var service = new StockTransferService(context, NullLogger<StockTransferService>.Instance);

            var transfer = new StockTransfer
            {
                TransferCode = "TRF-TEST-01",
                SourceStoreId = source.Id,
                DestinationStoreId = dest.Id,
                CreatedByUserId = user.Id,
                Status = "IN_TRANSIT", // Đang vận chuyển
                TotalRequestedQuantity = 10,
                TotalSentQuantity = 10
            };
            context.StockTransfers.Add(transfer);
            await context.SaveChangesAsync();

            // Act: Hủy phiếu khi đang IN_TRANSIT
            Func<Task> act = async () => await service.CancelTransferAsync(transfer.Id, user.Id, "Hủy");

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Chỉ có thể hủy phiếu khi còn ở trạng thái PENDING*");
        }

        [Fact]
        public async Task ReceiveTransfer_NotTransit_ThrowsException()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var (source, dest, user, product, _) = await SetupEntitiesAsync(context);
            var service = new StockTransferService(context, NullLogger<StockTransferService>.Instance);

            var transfer = new StockTransfer
            {
                TransferCode = "TRF-TEST-02",
                SourceStoreId = source.Id,
                DestinationStoreId = dest.Id,
                CreatedByUserId = user.Id,
                Status = "PENDING", // Chưa xuất kho
                TotalRequestedQuantity = 10
            };
            context.StockTransfers.Add(transfer);
            await context.SaveChangesAsync();

            var dto = new ReceiveTransferDto
            {
                TransferId = transfer.Id,
                Items = new List<ReceiveTransferItemDto>()
            };

            // Act
            Func<Task> act = async () => await service.ReceiveAndImportAsync(dto, user.Id);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Chỉ phiếu ở trạng thái IN_TRANSIT mới có thể xác nhận nhập kho*");
        }

        [Fact]
        public async Task ReceiveTransfer_Success_SetsStatusToReceived()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var (source, dest, user, product, batch) = await SetupEntitiesAsync(context);
            var service = new StockTransferService(context, NullLogger<StockTransferService>.Instance);

            var transfer = new StockTransfer
            {
                TransferCode = "TRF-TEST-03",
                SourceStoreId = source.Id,
                DestinationStoreId = dest.Id,
                CreatedByUserId = user.Id,
                Status = "IN_TRANSIT",
                TotalRequestedQuantity = 10,
                TotalSentQuantity = 10
            };
            var detail = new StockTransferDetail
            {
                ProductId = product.Id,
                ProductBatchId = batch.Id,
                RequestedQuantity = 10,
                SentQuantity = 10,
                ReceivedQuantity = 0
            };
            transfer.Details.Add(detail);
            context.StockTransfers.Add(transfer);
            await context.SaveChangesAsync();

            var receiveDto = new ReceiveTransferDto
            {
                TransferId = transfer.Id,
                Note = "Nhận đủ 10 hộp nguyên vẹn",
                Items = new List<ReceiveTransferItemDto>
                {
                    new ReceiveTransferItemDto { DetailId = detail.Id, ReceivedQuantity = 10 }
                }
            };

            // Act
            var result = await service.ReceiveAndImportAsync(receiveDto, user.Id);

            // Assert
            result.Status.Should().Be("RECEIVED");
            result.TotalReceivedQuantity.Should().Be(10);
            result.ReceivedByUserName.Should().Be("Nhân Viên Kho");
        }
    }
}
