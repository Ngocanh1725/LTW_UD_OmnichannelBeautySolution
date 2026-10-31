using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Omnichannel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDomainEventsOutboxAndRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StoreInventories",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ColorHex",
                table: "ProductVariants",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(7)",
                oldMaxLength: 7,
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceAdjustment",
                table: "ProductVariants",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "StockQuantity",
                table: "ProductVariants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SwatchColorHex",
                table: "ProductVariants",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SwatchImageUrl",
                table: "ProductVariants",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightGrams",
                table: "ProductVariants",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BrandId",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CosmeticNotificationExpiry",
                table: "Products",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CosmeticNotificationNo",
                table: "Products",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PAOMonths",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageCondition",
                table: "Products",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "AdminRepliedAt",
                table: "ProductReviews",
                type: "datetime2(7)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminReply",
                table: "ProductReviews",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminReplyBy",
                table: "ProductReviews",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HelpfulCount",
                table: "ProductReviews",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Longevity",
                table: "ProductReviews",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Scent",
                table: "ProductReviews",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SkinFeel",
                table: "ProductReviews",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AlertDaysThreshold",
                table: "ProductBatches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ClearanceDiscountPercent",
                table: "ProductBatches",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gs1ApplicationIdentifier",
                table: "ProductBatches",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductVariantId",
                table: "ProductBatches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QualityCertificateUrl",
                table: "ProductBatches",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ProductBatches",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PosShifts",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Orders",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "LoyaltyWallets",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Coupons",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "Categories",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    StoreId = table.Column<int>(type: "int", nullable: true),
                    ActionUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    EntityKey = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alerts_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Alerts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Brands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    CountryOfOrigin = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LogoUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionKey = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CustomerEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastMessageAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MessageCount = table.Column<int>(type: "int", nullable: false),
                    DetectedTopic = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdempotencyKey = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    RequestPath = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    RequestPayloadHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    ResponseStatusCode = table.Column<int>(type: "int", nullable: false),
                    ResponseBodyJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InternalMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SenderId = table.Column<int>(type: "int", nullable: false),
                    ReceiverId = table.Column<int>(type: "int", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    IsDeletedBySender = table.Column<bool>(type: "bit", nullable: false),
                    IsDeletedByReceiver = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternalMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InternalMessages_Users_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InternalMessages_Users_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StoreId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Url = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventType = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    NextRetryAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductAttributes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    DisplayType = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAttributes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkinConcerns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Icon = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkinConcerns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockAlerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductVariantId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    NotifiedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockAlerts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockAlerts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WishlistItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WishlistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WishlistItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WishlistItems_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionKey = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    SessionId = table.Column<int>(type: "int", nullable: true),
                    Sender = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Intent = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_ChatSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "ChatSessions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductAttributeValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductAttributeId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ColorHex = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    ImageUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAttributeValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductAttributeValues_ProductAttributes_ProductAttributeId",
                        column: x => x.ProductAttributeId,
                        principalTable: "ProductAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductConcerns",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    SkinConcernId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductConcerns", x => new { x.ProductId, x.SkinConcernId });
                    table.ForeignKey(
                        name: "FK_ProductConcerns_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductConcerns_SkinConcerns_SkinConcernId",
                        column: x => x.SkinConcernId,
                        principalTable: "SkinConcerns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariantAttributeValues",
                columns: table => new
                {
                    ProductVariantId = table.Column<int>(type: "int", nullable: false),
                    ProductAttributeValueId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantAttributeValues", x => new { x.ProductVariantId, x.ProductAttributeValueId });
                    table.ForeignKey(
                        name: "FK_ProductVariantAttributeValues_ProductAttributeValues_ProductAttributeValueId",
                        column: x => x.ProductAttributeValueId,
                        principalTable: "ProductAttributeValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductVariantAttributeValues_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_BrandId",
                table: "Products",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBatches_ProductVariantId",
                table: "ProductBatches",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "UQ_ProductBatches_Gs1AI",
                table: "ProductBatches",
                column: "Gs1ApplicationIdentifier",
                unique: true,
                filter: "[Gs1ApplicationIdentifier] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreatedAt",
                table: "Orders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_IsRead_CreatedAt",
                table: "Alerts",
                columns: new[] { "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_StoreId",
                table: "Alerts",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_Type_EntityKey",
                table: "Alerts",
                columns: new[] { "Type", "EntityKey" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_UserId",
                table: "Alerts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SessionId",
                table: "ChatMessages",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_Key",
                table: "IdempotencyRecords",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InternalMessages_Receiver",
                table: "InternalMessages",
                columns: new[] { "ReceiverId", "IsRead", "IsDeletedByReceiver" });

            migrationBuilder.CreateIndex(
                name: "IX_InternalMessages_Sender",
                table: "InternalMessages",
                columns: new[] { "SenderId", "IsDeletedBySender" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Role_IsRead",
                table: "Notifications",
                columns: new[] { "Role", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_StoreId_IsRead",
                table: "Notifications",
                columns: new[] { "StoreId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Type",
                table: "Notifications",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Processing",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAtUtc", "RetryCount" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_Code",
                table: "ProductAttributes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeValues_ProductAttributeId",
                table: "ProductAttributeValues",
                column: "ProductAttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductConcerns_SkinConcernId",
                table: "ProductConcerns",
                column: "SkinConcernId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantAttributeValues_ProductAttributeValueId",
                table: "ProductVariantAttributeValues",
                column: "ProductAttributeValueId");

            migrationBuilder.CreateIndex(
                name: "IX_SkinConcerns_Slug",
                table: "SkinConcerns",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockAlerts_ProductId",
                table: "StockAlerts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAlerts_UserId",
                table: "StockAlerts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_ProductId",
                table: "WishlistItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_UserId",
                table: "WishlistItems",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductBatches_ProductVariants_ProductVariantId",
                table: "ProductBatches",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Brands_BrandId",
                table: "Products",
                column: "BrandId",
                principalTable: "Brands",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductBatches_ProductVariants_ProductVariantId",
                table: "ProductBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Brands_BrandId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "Brands");

            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "IdempotencyRecords");

            migrationBuilder.DropTable(
                name: "InternalMessages");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "ProductConcerns");

            migrationBuilder.DropTable(
                name: "ProductVariantAttributeValues");

            migrationBuilder.DropTable(
                name: "StockAlerts");

            migrationBuilder.DropTable(
                name: "WishlistItems");

            migrationBuilder.DropTable(
                name: "ChatSessions");

            migrationBuilder.DropTable(
                name: "SkinConcerns");

            migrationBuilder.DropTable(
                name: "ProductAttributeValues");

            migrationBuilder.DropTable(
                name: "ProductAttributes");

            migrationBuilder.DropIndex(
                name: "IX_Products_BrandId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_ProductBatches_ProductVariantId",
                table: "ProductBatches");

            migrationBuilder.DropIndex(
                name: "UQ_ProductBatches_Gs1AI",
                table: "ProductBatches");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CreatedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StoreInventories");

            migrationBuilder.DropColumn(
                name: "PriceAdjustment",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "StockQuantity",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "SwatchColorHex",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "SwatchImageUrl",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "WeightGrams",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "BrandId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CosmeticNotificationExpiry",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CosmeticNotificationNo",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PAOMonths",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StorageCondition",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "AdminRepliedAt",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "AdminReply",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "AdminReplyBy",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "HelpfulCount",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "Longevity",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "Scent",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "SkinFeel",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "AlertDaysThreshold",
                table: "ProductBatches");

            migrationBuilder.DropColumn(
                name: "ClearanceDiscountPercent",
                table: "ProductBatches");

            migrationBuilder.DropColumn(
                name: "Gs1ApplicationIdentifier",
                table: "ProductBatches");

            migrationBuilder.DropColumn(
                name: "ProductVariantId",
                table: "ProductBatches");

            migrationBuilder.DropColumn(
                name: "QualityCertificateUrl",
                table: "ProductBatches");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ProductBatches");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PosShifts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "LoyaltyWallets");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "Categories");

            migrationBuilder.AlterColumn<string>(
                name: "ColorHex",
                table: "ProductVariants",
                type: "varchar(7)",
                maxLength: 7,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldNullable: true);
        }
    }
}
