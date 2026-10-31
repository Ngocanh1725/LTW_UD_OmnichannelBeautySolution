using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Inventory
{
    public class StockTransferDetailDto
    {
        public int Id { get; set; }
        public int StockTransferId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductSku { get; set; }
        public string? ProductBarcode { get; set; }
        public string? ProductThumbnailUrl { get; set; }
        public int? ProductBatchId { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpDate { get; set; }
        public int RequestedQuantity { get; set; }
        public int SentQuantity { get; set; }
        public int ReceivedQuantity { get; set; }
        public int ShortageQuantity => Math.Max(0, SentQuantity - ReceivedQuantity);
        public string? ShortageReason { get; set; }
        public string? Note { get; set; }
    }

    public class StockTransferDto
    {
        public int Id { get; set; }
        public string TransferCode { get; set; } = string.Empty;
        public int SourceStoreId { get; set; }
        public string SourceStoreName { get; set; } = string.Empty;
        public int DestinationStoreId { get; set; }
        public string DestinationStoreName { get; set; } = string.Empty;
        public int CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public int? ApprovedByUserId { get; set; }
        public string? ApprovedByUserName { get; set; }
        public int? ReceivedByUserId { get; set; }
        public string? ReceivedByUserName { get; set; }
        public string Status { get; set; } = "PENDING"; // PENDING, APPROVED, IN_TRANSIT, RECEIVED, CANCELLED
        public int TotalRequestedQuantity { get; set; }
        public int TotalSentQuantity { get; set; }
        public int TotalReceivedQuantity { get; set; }
        public int TotalShortageQuantity => Math.Max(0, TotalSentQuantity - TotalReceivedQuantity);
        public string? Note { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public List<StockTransferDetailDto> Details { get; set; } = new();
    }

    public class CreateStockTransferItemDto
    {
        public int ProductId { get; set; }
        public int RequestedQuantity { get; set; }
        public string? Note { get; set; }
    }

    public class CreateStockTransferDto
    {
        public int SourceStoreId { get; set; }
        public int DestinationStoreId { get; set; }
        public string? Note { get; set; }
        public List<CreateStockTransferItemDto> Items { get; set; } = new();
    }

    public class ApproveTransferDto
    {
        public int TransferId { get; set; }
        public string? Note { get; set; }
    }

    public class ReceiveTransferItemDto
    {
        public int DetailId { get; set; }
        public int ProductId { get; set; }
        public int ReceivedQuantity { get; set; }
        public string? ShortageReason { get; set; }
    }

    public class ReceiveTransferDto
    {
        public int TransferId { get; set; }
        public string? Note { get; set; }
        public List<ReceiveTransferItemDto> Items { get; set; } = new();
    }

    public class StockTransferFilterDto
    {
        public string? Status { get; set; }
        public int? SourceStoreId { get; set; }
        public int? DestinationStoreId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Keyword { get; set; }
    }
}
