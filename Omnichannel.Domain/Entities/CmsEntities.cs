using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Omnichannel.Domain.Entities
{
    [Table("Menus")]
    public class Menu
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string Code { get; set; } = string.Empty; // e.g., HEADER_MAIN, FOOTER_MAIN

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string Position { get; set; } = "TOP";

        public bool IsActive { get; set; } = true;

        public virtual ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    }

    [Table("MenuItems")]
    public class MenuItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int MenuId { get; set; }
        public int? ParentId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column(TypeName = "varchar(255)")]
        public string Url { get; set; } = "#";

        [MaxLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string? Icon { get; set; }

        public int SortOrder { get; set; } = 0;

        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string Target { get; set; } = "_self"; // _self, _blank

        public bool IsActive { get; set; } = true;

        public virtual Menu Menu { get; set; } = null!;
        public virtual MenuItem? Parent { get; set; }
        public virtual ICollection<MenuItem> Children { get; set; } = new List<MenuItem>();
    }

    [Table("Banners")]
    public class Banner
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Subtitle { get; set; }

        [Required]
        [MaxLength(500)]
        [Column(TypeName = "varchar(500)")]
        public string ImageUrl { get; set; } = string.Empty;

        [MaxLength(255)]
        [Column(TypeName = "varchar(255)")]
        public string? LinkUrl { get; set; }

        [MaxLength(50)]
        public string? ButtonText { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string Position { get; set; } = "HERO_HOME"; // HERO_HOME, MID_PROMO, SIDEBAR

        public int SortOrder { get; set; } = 0;
        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public int TrackingClickCount { get; set; } = 0;

        public string? DesktopImageUrl { get; set; }
        public string? MobileImageUrl { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime? ValidFrom { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime? ValidTo { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime? StartDate { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime? EndDate { get; set; }
    }

    [Table("PostCategories")]
    public class PostCategory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        [Column(TypeName = "varchar(150)")]
        public string Slug { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    }

    [Table("Posts")]
    public class Post
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int PostCategoryId { get; set; }
        public int? AuthorId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column(TypeName = "varchar(255)")]
        public string Slug { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Summary { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [MaxLength(500)]
        [Column(TypeName = "varchar(500)")]
        public string? ThumbnailUrl { get; set; }

        public int ViewCount { get; set; } = 0;
        public bool IsPublished { get; set; } = true;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? PublishedAt { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual PostCategory PostCategory { get; set; } = null!;
        public virtual User? Author { get; set; }
    }

    [Table("StaticPages")]
    public class StaticPage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        [Column(TypeName = "varchar(200)")]
        public string Slug { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? MetaTitle { get; set; }

        [MaxLength(500)]
        public string? MetaDescription { get; set; }

        public bool IsActive { get; set; } = true;

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? UpdatedAt { get; set; }
    }
}
