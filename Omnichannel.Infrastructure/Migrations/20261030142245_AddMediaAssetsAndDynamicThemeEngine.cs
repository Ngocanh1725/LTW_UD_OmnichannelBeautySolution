using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Omnichannel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaAssetsAndDynamicThemeEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AvatarMediaId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ThumbnailMediaId",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ThumbnailMediaId",
                table: "Posts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MediaAssetId",
                table: "Categories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LogoMediaId",
                table: "Brands",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DesktopMediaId",
                table: "Banners",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MobileMediaId",
                table: "Banners",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MediaAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Data = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    AltText = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ChecksumHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    Category = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaAssets_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ThemeConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreId = table.Column<int>(type: "int", nullable: true),
                    BrandName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slogan = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PrimaryColor = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    SecondaryColor = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    AccentColor = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    FontFamily = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    LogoMediaId = table.Column<int>(type: "int", nullable: true),
                    FaviconMediaId = table.Column<int>(type: "int", nullable: true),
                    AnnouncementBarText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AnnouncementBarActive = table.Column<bool>(type: "bit", nullable: false),
                    AnnouncementBarBgColor = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Hotline = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FooterCopyright = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CustomCss = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThemeConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThemeConfigs_MediaAssets_FaviconMediaId",
                        column: x => x.FaviconMediaId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ThemeConfigs_MediaAssets_LogoMediaId",
                        column: x => x.LogoMediaId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UiSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SectionCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Subtitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    BackgroundMediaId = table.Column<int>(type: "int", nullable: true),
                    LayoutType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    ConfigurationJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UiSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UiSections_MediaAssets_BackgroundMediaId",
                        column: x => x.BackgroundMediaId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_AvatarMediaId",
                table: "Users",
                column: "AvatarMediaId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ThumbnailMediaId",
                table: "Products",
                column: "ThumbnailMediaId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_ThumbnailMediaId",
                table: "Posts",
                column: "ThumbnailMediaId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_MediaAssetId",
                table: "Categories",
                column: "MediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_LogoMediaId",
                table: "Brands",
                column: "LogoMediaId");

            migrationBuilder.CreateIndex(
                name: "IX_Banners_DesktopMediaId",
                table: "Banners",
                column: "DesktopMediaId");

            migrationBuilder.CreateIndex(
                name: "IX_Banners_MobileMediaId",
                table: "Banners",
                column: "MobileMediaId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_Category",
                table: "MediaAssets",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_ChecksumHash",
                table: "MediaAssets",
                column: "ChecksumHash");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_CreatedBy",
                table: "MediaAssets",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ThemeConfigs_FaviconMediaId",
                table: "ThemeConfigs",
                column: "FaviconMediaId");

            migrationBuilder.CreateIndex(
                name: "IX_ThemeConfigs_LogoMediaId",
                table: "ThemeConfigs",
                column: "LogoMediaId");

            migrationBuilder.CreateIndex(
                name: "IX_UiSections_BackgroundMediaId",
                table: "UiSections",
                column: "BackgroundMediaId");

            migrationBuilder.CreateIndex(
                name: "IX_UiSections_SectionCode",
                table: "UiSections",
                column: "SectionCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Banners_MediaAssets_DesktopMediaId",
                table: "Banners",
                column: "DesktopMediaId",
                principalTable: "MediaAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Banners_MediaAssets_MobileMediaId",
                table: "Banners",
                column: "MobileMediaId",
                principalTable: "MediaAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Brands_MediaAssets_LogoMediaId",
                table: "Brands",
                column: "LogoMediaId",
                principalTable: "MediaAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_MediaAssets_MediaAssetId",
                table: "Categories",
                column: "MediaAssetId",
                principalTable: "MediaAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_MediaAssets_ThumbnailMediaId",
                table: "Posts",
                column: "ThumbnailMediaId",
                principalTable: "MediaAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_MediaAssets_ThumbnailMediaId",
                table: "Products",
                column: "ThumbnailMediaId",
                principalTable: "MediaAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_MediaAssets_AvatarMediaId",
                table: "Users",
                column: "AvatarMediaId",
                principalTable: "MediaAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Banners_MediaAssets_DesktopMediaId",
                table: "Banners");

            migrationBuilder.DropForeignKey(
                name: "FK_Banners_MediaAssets_MobileMediaId",
                table: "Banners");

            migrationBuilder.DropForeignKey(
                name: "FK_Brands_MediaAssets_LogoMediaId",
                table: "Brands");

            migrationBuilder.DropForeignKey(
                name: "FK_Categories_MediaAssets_MediaAssetId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_MediaAssets_ThumbnailMediaId",
                table: "Posts");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_MediaAssets_ThumbnailMediaId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_MediaAssets_AvatarMediaId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "ThemeConfigs");

            migrationBuilder.DropTable(
                name: "UiSections");

            migrationBuilder.DropTable(
                name: "MediaAssets");

            migrationBuilder.DropIndex(
                name: "IX_Users_AvatarMediaId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Products_ThumbnailMediaId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Posts_ThumbnailMediaId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Categories_MediaAssetId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Brands_LogoMediaId",
                table: "Brands");

            migrationBuilder.DropIndex(
                name: "IX_Banners_DesktopMediaId",
                table: "Banners");

            migrationBuilder.DropIndex(
                name: "IX_Banners_MobileMediaId",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "AvatarMediaId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ThumbnailMediaId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ThumbnailMediaId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "MediaAssetId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "LogoMediaId",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "DesktopMediaId",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "MobileMediaId",
                table: "Banners");
        }
    }
}
