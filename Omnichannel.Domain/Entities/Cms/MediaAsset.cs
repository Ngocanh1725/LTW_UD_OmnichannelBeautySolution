using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Omnichannel.Domain.Entities
{
    public partial class MediaAsset
    {
        public Guid AssetUid { get; set; } = Guid.NewGuid();

        [MaxLength(500)]
        public string? StoragePath { get; set; }

        [MaxLength(500)]
        public string? PublicUrl { get; set; }

        public long? FileSizeBytes { get; set; }

        [MaxLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string? MimeType { get; set; }

        public int? UploadedByStaffId { get; set; }
        public virtual User? UploadedByStaff { get; set; }
    }
}

