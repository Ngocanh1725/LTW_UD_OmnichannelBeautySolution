using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Omnichannel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicCmsAndEnterpriseTheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FaviconMediaAssetId",
                table: "ThemeConfigs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FontBody",
                table: "ThemeConfigs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FontHeading",
                table: "ThemeConfigs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "LogoMediaAssetId",
                table: "ThemeConfigs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThemeName",
                table: "ThemeConfigs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "AssetUid",
                table: "MediaAssets",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "MediaAssets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MimeType",
                table: "MediaAssets",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicUrl",
                table: "MediaAssets",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoragePath",
                table: "MediaAssets",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UploadedByStaffId",
                table: "MediaAssets",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PageLayouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PageKey = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MetaDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    DraftConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublishedConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageLayouts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LayoutSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PageLayoutId = table.Column<int>(type: "int", nullable: false),
                    SectionKey = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    CustomStylesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LayoutSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LayoutSections_PageLayouts_PageLayoutId",
                        column: x => x.PageLayoutId,
                        principalTable: "PageLayouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SectionItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LayoutSectionId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Subtitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TargetUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    MediaAssetId = table.Column<int>(type: "int", nullable: true),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SectionItems_LayoutSections_LayoutSectionId",
                        column: x => x.LayoutSectionId,
                        principalTable: "LayoutSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SectionItems_MediaAssets_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ThemeConfigs_FaviconMediaAssetId",
                table: "ThemeConfigs",
                column: "FaviconMediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ThemeConfigs_LogoMediaAssetId",
                table: "ThemeConfigs",
                column: "LogoMediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_UploadedByStaffId",
                table: "MediaAssets",
                column: "UploadedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_LayoutSections_PageLayout_Order",
                table: "LayoutSections",
                columns: new[] { "PageLayoutId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_PageLayouts_PageKey",
                table: "PageLayouts",
                column: "PageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SectionItems_MediaAssetId",
                table: "SectionItems",
                column: "MediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_SectionItems_Section_Order",
                table: "SectionItems",
                columns: new[] { "LayoutSectionId", "OrderIndex" });

            migrationBuilder.AddForeignKey(
                name: "FK_MediaAssets_Users_UploadedByStaffId",
                table: "MediaAssets",
                column: "UploadedByStaffId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ThemeConfigs_MediaAssets_FaviconMediaAssetId",
                table: "ThemeConfigs",
                column: "FaviconMediaAssetId",
                principalTable: "MediaAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ThemeConfigs_MediaAssets_LogoMediaAssetId",
                table: "ThemeConfigs",
                column: "LogoMediaAssetId",
                principalTable: "MediaAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MediaAssets_Users_UploadedByStaffId",
                table: "MediaAssets");

            migrationBuilder.DropForeignKey(
                name: "FK_ThemeConfigs_MediaAssets_FaviconMediaAssetId",
                table: "ThemeConfigs");

            migrationBuilder.DropForeignKey(
                name: "FK_ThemeConfigs_MediaAssets_LogoMediaAssetId",
                table: "ThemeConfigs");

            migrationBuilder.DropTable(
                name: "SectionItems");

            migrationBuilder.DropTable(
                name: "LayoutSections");

            migrationBuilder.DropTable(
                name: "PageLayouts");

            migrationBuilder.DropIndex(
                name: "IX_ThemeConfigs_FaviconMediaAssetId",
                table: "ThemeConfigs");

            migrationBuilder.DropIndex(
                name: "IX_ThemeConfigs_LogoMediaAssetId",
                table: "ThemeConfigs");

            migrationBuilder.DropIndex(
                name: "IX_MediaAssets_UploadedByStaffId",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "FaviconMediaAssetId",
                table: "ThemeConfigs");

            migrationBuilder.DropColumn(
                name: "FontBody",
                table: "ThemeConfigs");

            migrationBuilder.DropColumn(
                name: "FontHeading",
                table: "ThemeConfigs");

            migrationBuilder.DropColumn(
                name: "LogoMediaAssetId",
                table: "ThemeConfigs");

            migrationBuilder.DropColumn(
                name: "ThemeName",
                table: "ThemeConfigs");

            migrationBuilder.DropColumn(
                name: "AssetUid",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "MimeType",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "PublicUrl",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "StoragePath",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "UploadedByStaffId",
                table: "MediaAssets");
        }
    }
}
