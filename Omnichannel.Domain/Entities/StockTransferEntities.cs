using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Omnichannel.Domain.Common;
//Quản lý phiếu điều chuyển hàng hóa qua lại giữa các chi nhánh (Ví dụ: Chuyển từ Kho Tổng sang Chi nhánh Quận 1).
namespace Omnichannel.Domain.Entities
{
    /// <summary>
    /// Phiếu điều chuyển kho liên chi nhánh (Kho Tổng <-> Chi nhánh, hoặc Chi nhánh <-> Chi nhánh)
    /// Vòng đời trạng thái: PENDING -> APPROVED -> IN_TRANSIT -> RECEIVED (hoặc CANCELLED)
    /// </summary>
    [Table("StockTransfers")]
    public class StockTransfer : AggregateRoot
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string TransferCode { get; set; } = string.Empty; // e.g., TRF-20260930-001

        public int SourceStoreId { get; set; } // Kho nguồn xuất hàng

        public int DestinationStoreId { get; set; } // Kho đích nhập hàng

        public int CreatedByUserId { get; set; } // Người lập yêu cầu chuyển kho

        public int? ApprovedByUserId { get; set; } // Quản trị viên/Quản lý kho duyệt xuất

        public int? ReceivedByUserId { get; set; } // Thủ kho đích xác nhận thực nhận

        [Required]
        [MaxLength(30)]
        [Column(TypeName = "varchar(30)")]
        public string Status { get; set; } = "PENDING"; // PENDING, APPROVED, IN_TRANSIT, RECEIVED, CANCELLED

        public int TotalRequestedQuantity { get; set; } // Tổng số lượng yêu cầu chuyển

        public int TotalSentQuantity { get; set; } // Tổng số lượng thực xuất từ kho nguồn

        public int TotalReceivedQuantity { get; set; } // Tổng số lượng thực nhận tại kho đích

        [MaxLength(500)]
        public string? Note { get; set; } // Ghi chú yêu cầu chuyển kho

        [MaxLength(500)]
        public string? CancellationReason { get; set; } // Lý do hủy nếu bị từ chối

        [Column(TypeName = "datetime2(7)")]
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? ApprovedAt { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime? ReceivedAt { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? UpdatedAt { get; set; }

        // Navigations
        public virtual Store SourceStore { get; set; } = null!;
        public virtual Store DestinationStore { get; set; } = null!;
        public virtual User CreatedByUser { get; set; } = null!;
        public virtual User? ApprovedByUser { get; set; }
        public virtual User? ReceivedByUser { get; set; }
        public virtual ICollection<StockTransferDetail> Details { get; set; } = new List<StockTransferDetail>();
    }

    /// <summary>
    /// Dòng chi tiết phiếu chuyển kho theo từng sản phẩm và phân bổ lô theo thuật toán FEFO
    /// </summary>
    [Table("StockTransferDetails")]
    public class StockTransferDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int StockTransferId { get; set; }

        public int ProductId { get; set; }

        public int? ProductBatchId { get; set; } // Lô hàng được xuất theo FEFO (cận date xuất trước)

        public int RequestedQuantity { get; set; } // Số lượng chi nhánh yêu cầu

        public int SentQuantity { get; set; } // Số lượng kho nguồn thực xuất

        public int ReceivedQuantity { get; set; } // Số lượng kho đích thực nhận sau kiểm đếm

        public int ShortageQuantity => Math.Max(0, SentQuantity - ReceivedQuantity); // Số lượng thiếu hụt

        [MaxLength(255)]
        public string? ShortageReason { get; set; } // Ghi chú lý do thiếu hụt (vỡ, móp méo, thất lạc...)

        [MaxLength(255)]
        public string? Note { get; set; }

        // Navigations
        public virtual StockTransfer StockTransfer { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
        public virtual ProductBatch? ProductBatch { get; set; }
    }
}
