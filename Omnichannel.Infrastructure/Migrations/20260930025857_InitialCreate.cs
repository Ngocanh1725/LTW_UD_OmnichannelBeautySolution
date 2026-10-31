using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Omnichannel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActiveIngredients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActiveIngredients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Action = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Banners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Subtitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ImageUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    LinkUrl = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    ButtonText = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Position = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TrackingClickCount = table.Column<int>(type: "int", nullable: false),
                    DesktopImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MobileImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Banners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ImageUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Categories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CodReconciliations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReconciliationCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    ShippingPartner = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    ReconciliationDate = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    TotalOrders = table.Column<int>(type: "int", nullable: false),
                    TotalCodAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReceivedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Difference = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodReconciliations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Coupons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CouponCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DiscountType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinOrderValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaxDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UsageLimit = table.Column<int>(type: "int", nullable: true),
                    UsageLimitPerUser = table.Column<int>(type: "int", nullable: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coupons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MemberTiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TierName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MinSpend = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxSpend = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PointMultiplier = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    BirthdayBonusPoints = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    BadgeIconUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberTiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Menus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Position = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Menus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Module = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PostCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Promotions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PromotionCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PromotionType = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinOrderValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaxDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UsageLimit = table.Column<int>(type: "int", nullable: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AppliesTo = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promotions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkinTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Slug = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkinTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SlugRedirects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OldSlug = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    NewSlug = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    EntityType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlugRedirects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StaticPages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetaTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaticPages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Area = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsHeadquarters = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MenuId = table.Column<int>(type: "int", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Url = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Icon = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Target = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LinkType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "CUSTOM"),
                    TargetEntityId = table.Column<int>(type: "int", nullable: true),
                    TargetSlug = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    CssClass = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItems_MenuItems_ParentId",
                        column: x => x.ParentId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MenuItems_Menus_MenuId",
                        column: x => x.MenuId,
                        principalTable: "Menus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    AvatarUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    NormalizedUsername = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserType = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    StoreId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: true),
                    Sku = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Barcode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false),
                    ShortDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CostPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SellingPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    ImagesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    HasVariants = table.Column<bool>(type: "bit", nullable: false),
                    ProductType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    CosmeticNotificationNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    ManufacturerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CountryOfOrigin = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ImporterName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.CheckConstraint("CK_Products_CostPrice_NonNegative", "[CostPrice] >= 0");
                    table.CheckConstraint("CK_Products_SellingPrice_NonNegative", "[SellingPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Products_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierDebts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    PurchaseOrderCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    DebtAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierDebts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierDebts_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Carts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    CartSessionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Carts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    Gender = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true),
                    CurrentSkinType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SkinConcerns = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    KnownAllergies = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PreferredBrands = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LastConsultationDate = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    ConsultantUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerProfiles_Users_ConsultantUserId",
                        column: x => x.ConsultantUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CustomerProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyWallets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CurrentTierId = table.Column<int>(type: "int", nullable: false),
                    TotalPointsEarned = table.Column<int>(type: "int", nullable: false),
                    CurrentBalance = table.Column<int>(type: "int", nullable: false),
                    TotalSpend12Months = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MemberSince = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    LastTierEvaluationDate = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    QrCodeData = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyWallets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoyaltyWallets_MemberTiers_CurrentTierId",
                        column: x => x.CurrentTierId,
                        principalTable: "MemberTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoyaltyWallets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PosShifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShiftCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    CashierId = table.Column<int>(type: "int", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExpectedCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActualCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Difference = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CashSalesTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CardSalesTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransactionCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PosShifts_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosShifts_Users_CashierId",
                        column: x => x.CashierId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Posts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostCategoryId = table.Column<int>(type: "int", nullable: false),
                    AuthorId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Slug = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    ViewCount = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Posts_PostCategories_PostCategoryId",
                        column: x => x.PostCategoryId,
                        principalTable: "PostCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Posts_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SettingKey = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    SettingValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SettingGroup = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DataType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    IsEncrypted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Settings_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "StockTransfers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransferCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    SourceStoreId = table.Column<int>(type: "int", nullable: false),
                    DestinationStoreId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReceivedByUserId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    TotalRequestedQuantity = table.Column<int>(type: "int", nullable: false),
                    TotalSentQuantity = table.Column<int>(type: "int", nullable: false),
                    TotalReceivedQuantity = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransfers_Stores_DestinationStoreId",
                        column: x => x.DestinationStoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransfers_Stores_SourceStoreId",
                        column: x => x.SourceStoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransfers_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransfers_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransfers_Users_ReceivedByUserId",
                        column: x => x.ReceivedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserPermissions",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    IsGranted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissions", x => new { x.UserId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_UserPermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPermissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    BatchNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    ManufacturingDate = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    ExpDate = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    ImportCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InitialQuantity = table.Column<int>(type: "int", nullable: false),
                    CurrentQuantity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductBatches", x => x.Id);
                    table.CheckConstraint("CK_ProductBatches_ExpDate_ManufacturingDate", "[ExpDate] > [ManufacturingDate]");
                    table.ForeignKey(
                        name: "FK_ProductBatches_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductIngredients",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ActiveIngredientId = table.Column<int>(type: "int", nullable: false),
                    Concentration = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductIngredients", x => new { x.ProductId, x.ActiveIngredientId });
                    table.ForeignKey(
                        name: "FK_ProductIngredients_ActiveIngredients_ActiveIngredientId",
                        column: x => x.ActiveIngredientId,
                        principalTable: "ActiveIngredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductIngredients_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductSkinTypes",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    SkinTypeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSkinTypes", x => new { x.ProductId, x.SkinTypeId });
                    table.ForeignKey(
                        name: "FK_ProductSkinTypes_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductSkinTypes_SkinTypes_SkinTypeId",
                        column: x => x.SkinTypeId,
                        principalTable: "SkinTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Sku = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Barcode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    VariantName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ColorName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ColorHex = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: true),
                    Volume = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Formulation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CostPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SellingPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.Id);
                    table.CheckConstraint("CK_ProductVariants_SellingPrice_NonNegative", "[SellingPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_ProductVariants_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromotionItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PromotionId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    GiftProductId = table.Column<int>(type: "int", nullable: true),
                    GiftQuantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromotionItems_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromotionItems_Products_GiftProductId",
                        column: x => x.GiftProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromotionItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromotionItems_Promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "Promotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CartItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CartId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CartItems_Carts_CartId",
                        column: x => x.CartId,
                        principalTable: "Carts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CartItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepurchaseSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerProfileId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    LastPurchaseDate = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    EstimatedVolumeMl = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DailyUsageMl = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    EstimatedDepletionDate = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    IsNotified = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepurchaseSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepurchaseSchedules_CustomerProfiles_CustomerProfileId",
                        column: x => x.CustomerProfileId,
                        principalTable: "CustomerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RepurchaseSchedules_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SkinHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerProfileId = table.Column<int>(type: "int", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    SkinType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SkinCondition = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TreatmentNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RecordedBy = table.Column<int>(type: "int", nullable: true),
                    ImageUrls = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkinHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkinHistories_CustomerProfiles_CustomerProfileId",
                        column: x => x.CustomerProfileId,
                        principalTable: "CustomerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SkinHistories_Users_RecordedBy",
                        column: x => x.RecordedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    PosShiftId = table.Column<int>(type: "int", nullable: true),
                    OrderSource = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    OrderStatus = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    PaymentStatus = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    PaymentMethod = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ShippingFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CustomerPhone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    CustomerEmail = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    ShippingAddress = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_PosShifts_PosShiftId",
                        column: x => x.PosShiftId,
                        principalTable: "PosShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PosDrawerTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PosShiftId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PerformedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosDrawerTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PosDrawerTransactions_PosShifts_PosShiftId",
                        column: x => x.PosShiftId,
                        principalTable: "PosShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PosDrawerTransactions_Users_PerformedBy",
                        column: x => x.PerformedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    ProductBatchId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    TransactionType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    QuantityChanged = table.Column<int>(type: "int", nullable: false),
                    QuantityBefore = table.Column<int>(type: "int", nullable: false),
                    QuantityAfter = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryLogs_ProductBatches_ProductBatchId",
                        column: x => x.ProductBatchId,
                        principalTable: "ProductBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryLogs_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "StockTransferDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockTransferId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductBatchId = table.Column<int>(type: "int", nullable: true),
                    RequestedQuantity = table.Column<int>(type: "int", nullable: false),
                    SentQuantity = table.Column<int>(type: "int", nullable: false),
                    ReceivedQuantity = table.Column<int>(type: "int", nullable: false),
                    ShortageReason = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransferDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransferDetails_ProductBatches_ProductBatchId",
                        column: x => x.ProductBatchId,
                        principalTable: "ProductBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransferDetails_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransferDetails_StockTransfers_StockTransferId",
                        column: x => x.StockTransferId,
                        principalTable: "StockTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StoreInventories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    ProductBatchId = table.Column<int>(type: "int", nullable: false),
                    PhysicalQuantity = table.Column<int>(type: "int", nullable: false),
                    ReservedQuantity = table.Column<int>(type: "int", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreInventories", x => x.Id);
                    table.CheckConstraint("CK_StoreInventories_Physical_NonNegative", "[PhysicalQuantity] >= 0");
                    table.CheckConstraint("CK_StoreInventories_Physical_Reserved", "[PhysicalQuantity] >= [ReservedQuantity]");
                    table.CheckConstraint("CK_StoreInventories_Reserved_NonNegative", "[ReservedQuantity] >= 0");
                    table.ForeignKey(
                        name: "FK_StoreInventories_ProductBatches_ProductBatchId",
                        column: x => x.ProductBatchId,
                        principalTable: "ProductBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreInventories_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductSwatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductVariantId = table.Column<int>(type: "int", nullable: false),
                    SwatchImageUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    SwatchOnSkinImageUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSwatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductSwatches_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CouponUsages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CouponId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    DiscountApplied = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CouponUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CouponUsages_Coupons_CouponId",
                        column: x => x.CouponId,
                        principalTable: "Coupons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CouponUsages_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CouponUsages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FinancialLedgers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LedgerCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    EntryDate = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    EntryType = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    SupplierId = table.Column<int>(type: "int", nullable: true),
                    DebitAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreditAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialLedgers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialLedgers_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FinancialLedgers_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialLedgers_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FinancialLedgers_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CashierId = table.Column<int>(type: "int", nullable: true),
                    CustomerTaxCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_Users_CashierId",
                        column: x => x.CashierId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OrderDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductBatchId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderDetails_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderDetails_ProductBatches_ProductBatchId",
                        column: x => x.ProductBatchId,
                        principalTable: "ProductBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderDetails_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PointTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WalletId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    TransactionType = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    BalanceBefore = table.Column<int>(type: "int", nullable: false),
                    BalanceAfter = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PointTransactions_LoyaltyWallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "LoyaltyWallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PointTransactions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ProductReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ImagesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsVerifiedPurchase = table.Column<bool>(type: "bit", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    IsHidden = table.Column<bool>(type: "bit", nullable: false),
                    AdminNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SkinType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AgeRange = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductReviews", x => x.Id);
                    table.CheckConstraint("CK_ProductReviews_Rating_Range", "[Rating] >= 1 AND [Rating] <= 5");
                    table.ForeignKey(
                        name: "FK_ProductReviews_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProductReviews_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductReviews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Vouchers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    VoucherCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DiscountType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinOrderValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UsedOrderId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vouchers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vouchers_Orders_UsedOrderId",
                        column: x => x.UsedOrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Vouchers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActiveIngredients_Slug",
                table: "ActiveIngredients",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId",
                table: "CartItems",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ProductId",
                table: "CartItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_CartSessionId",
                table: "Carts",
                column: "CartSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_UserId",
                table: "Carts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentId",
                table: "Categories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                table: "Categories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CodReconciliations_ReconciliationCode",
                table: "CodReconciliations",
                column: "ReconciliationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_CouponCode",
                table: "Coupons",
                column: "CouponCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CouponUsages_CouponId",
                table: "CouponUsages",
                column: "CouponId");

            migrationBuilder.CreateIndex(
                name: "IX_CouponUsages_OrderId",
                table: "CouponUsages",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_CouponUsages_UserId",
                table: "CouponUsages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerProfiles_ConsultantUserId",
                table: "CustomerProfiles",
                column: "ConsultantUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerProfiles_UserId",
                table: "CustomerProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialLedgers_CreatedBy",
                table: "FinancialLedgers",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialLedgers_EntryDate",
                table: "FinancialLedgers",
                column: "EntryDate");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialLedgers_LedgerCode",
                table: "FinancialLedgers",
                column: "LedgerCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialLedgers_OrderId",
                table: "FinancialLedgers",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialLedgers_StoreId",
                table: "FinancialLedgers",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialLedgers_SupplierId",
                table: "FinancialLedgers",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLogs_Batch_Created",
                table: "InventoryLogs",
                columns: new[] { "ProductBatchId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLogs_Store_Created",
                table: "InventoryLogs",
                columns: new[] { "StoreId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLogs_UserId",
                table: "InventoryLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CashierId",
                table: "Invoices",
                column: "CashierId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceCode",
                table: "Invoices",
                column: "InvoiceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_OrderId",
                table: "Invoices",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyWallets_CurrentTierId",
                table: "LoyaltyWallets",
                column: "CurrentTierId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyWallets_UserId",
                table: "LoyaltyWallets",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberTiers_TierName",
                table: "MemberTiers",
                column: "TierName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_MenuId",
                table: "MenuItems",
                column: "MenuId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_ParentId",
                table: "MenuItems",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Menus_Code",
                table: "Menus",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_OrderId",
                table: "OrderDetails",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_ProductBatchId",
                table: "OrderDetails",
                column: "ProductBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_ProductId",
                table: "OrderDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderCode",
                table: "Orders",
                column: "OrderCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PosShiftId",
                table: "Orders",
                column: "PosShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                table: "Orders",
                columns: new[] { "OrderStatus", "PaymentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Store_Created",
                table: "Orders",
                columns: new[] { "StoreId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId",
                table: "Orders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                table: "Permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PointTransactions_OrderId",
                table: "PointTransactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PointTransactions_Wallet_Created",
                table: "PointTransactions",
                columns: new[] { "WalletId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PosDrawerTransactions_PerformedBy",
                table: "PosDrawerTransactions",
                column: "PerformedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PosDrawerTransactions_Shift_Created",
                table: "PosDrawerTransactions",
                columns: new[] { "PosShiftId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PosShifts_CashierId",
                table: "PosShifts",
                column: "CashierId");

            migrationBuilder.CreateIndex(
                name: "IX_PosShifts_ShiftCode",
                table: "PosShifts",
                column: "ShiftCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PosShifts_Store_Opened",
                table: "PosShifts",
                columns: new[] { "StoreId", "OpenedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PostCategories_Slug",
                table: "PostCategories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_AuthorId",
                table: "Posts",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_PostCategoryId",
                table: "Posts",
                column: "PostCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_Slug",
                table: "Posts",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductBatches_BatchNumber",
                table: "ProductBatches",
                column: "BatchNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBatches_FEFO_Lookup",
                table: "ProductBatches",
                columns: new[] { "ProductId", "ExpDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductIngredients_ActiveIngredientId",
                table: "ProductIngredients",
                column: "ActiveIngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_OrderId",
                table: "ProductReviews",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_Product_Approved_Created",
                table: "ProductReviews",
                columns: new[] { "ProductId", "IsApproved", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_UserId",
                table: "ProductReviews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Barcode",
                table: "Products",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Category_Active",
                table: "Products",
                columns: new[] { "CategoryId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Sku",
                table: "Products",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Slug",
                table: "Products",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_SupplierId",
                table: "Products",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSkinTypes_SkinTypeId",
                table: "ProductSkinTypes",
                column: "SkinTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSwatches_VariantId",
                table: "ProductSwatches",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_Barcode",
                table: "ProductVariants",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_Product_Active",
                table: "ProductVariants",
                columns: new[] { "ProductId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_Sku",
                table: "ProductVariants",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromotionItems_CategoryId",
                table: "PromotionItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionItems_GiftProductId",
                table: "PromotionItems",
                column: "GiftProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionItems_ProductId",
                table: "PromotionItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionItems_PromotionId",
                table: "PromotionItems",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_PromotionCode",
                table: "Promotions",
                column: "PromotionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepurchaseSchedules_ProductId",
                table: "RepurchaseSchedules",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_RepurchaseSchedules_Profile_Depletion",
                table: "RepurchaseSchedules",
                columns: new[] { "CustomerProfileId", "EstimatedDepletionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Settings_SettingKey",
                table: "Settings",
                column: "SettingKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Settings_UpdatedBy",
                table: "Settings",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SkinHistories_Profile_Recorded",
                table: "SkinHistories",
                columns: new[] { "CustomerProfileId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SkinHistories_RecordedBy",
                table: "SkinHistories",
                column: "RecordedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SkinTypes_Slug",
                table: "SkinTypes",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlugRedirects_EntityType_OldSlug",
                table: "SlugRedirects",
                columns: new[] { "EntityType", "OldSlug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaticPages_Slug",
                table: "StaticPages",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferDetails_ProductBatchId",
                table: "StockTransferDetails",
                column: "ProductBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferDetails_ProductId",
                table: "StockTransferDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferDetails_StockTransferId",
                table: "StockTransferDetails",
                column: "StockTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_ApprovedByUserId",
                table: "StockTransfers",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_CreatedByUserId",
                table: "StockTransfers",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_DestinationStoreId",
                table: "StockTransfers",
                column: "DestinationStoreId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_ReceivedByUserId",
                table: "StockTransfers",
                column: "ReceivedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_SourceStoreId",
                table: "StockTransfers",
                column: "SourceStoreId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_TransferCode",
                table: "StockTransfers",
                column: "TransferCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreInventories_ProductBatchId",
                table: "StoreInventories",
                column: "ProductBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreInventories_Store_Batch",
                table: "StoreInventories",
                columns: new[] { "StoreId", "ProductBatchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stores_Code",
                table: "Stores",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierDebts_PurchaseOrderCode",
                table: "SupplierDebts",
                column: "PurchaseOrderCode");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierDebts_SupplierId",
                table: "SupplierDebts",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Code",
                table: "Suppliers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_PermissionId",
                table: "UserPermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_StoreId",
                table: "Users",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_UsedOrderId",
                table: "Vouchers",
                column: "UsedOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_UserId",
                table: "Vouchers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_VoucherCode",
                table: "Vouchers",
                column: "VoucherCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Banners");

            migrationBuilder.DropTable(
                name: "CartItems");

            migrationBuilder.DropTable(
                name: "CodReconciliations");

            migrationBuilder.DropTable(
                name: "CouponUsages");

            migrationBuilder.DropTable(
                name: "FinancialLedgers");

            migrationBuilder.DropTable(
                name: "InventoryLogs");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "MenuItems");

            migrationBuilder.DropTable(
                name: "OrderDetails");

            migrationBuilder.DropTable(
                name: "PointTransactions");

            migrationBuilder.DropTable(
                name: "PosDrawerTransactions");

            migrationBuilder.DropTable(
                name: "Posts");

            migrationBuilder.DropTable(
                name: "ProductIngredients");

            migrationBuilder.DropTable(
                name: "ProductReviews");

            migrationBuilder.DropTable(
                name: "ProductSkinTypes");

            migrationBuilder.DropTable(
                name: "ProductSwatches");

            migrationBuilder.DropTable(
                name: "PromotionItems");

            migrationBuilder.DropTable(
                name: "RepurchaseSchedules");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "SkinHistories");

            migrationBuilder.DropTable(
                name: "SlugRedirects");

            migrationBuilder.DropTable(
                name: "StaticPages");

            migrationBuilder.DropTable(
                name: "StockTransferDetails");

            migrationBuilder.DropTable(
                name: "StoreInventories");

            migrationBuilder.DropTable(
                name: "SupplierDebts");

            migrationBuilder.DropTable(
                name: "UserPermissions");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Vouchers");

            migrationBuilder.DropTable(
                name: "Carts");

            migrationBuilder.DropTable(
                name: "Coupons");

            migrationBuilder.DropTable(
                name: "Menus");

            migrationBuilder.DropTable(
                name: "LoyaltyWallets");

            migrationBuilder.DropTable(
                name: "PostCategories");

            migrationBuilder.DropTable(
                name: "ActiveIngredients");

            migrationBuilder.DropTable(
                name: "SkinTypes");

            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.DropTable(
                name: "Promotions");

            migrationBuilder.DropTable(
                name: "CustomerProfiles");

            migrationBuilder.DropTable(
                name: "StockTransfers");

            migrationBuilder.DropTable(
                name: "ProductBatches");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "MemberTiers");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "PosShifts");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Stores");
        }
    }
}
