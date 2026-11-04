using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Omnichannel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelPendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UiSections_IsVisible_DisplayOrder",
                table: "UiSections",
                columns: new[] { "IsVisible", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive_IsFeatured",
                table: "Products",
                columns: new[] { "IsActive", "IsFeatured" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductBatches_ProductId_ExpDate",
                table: "ProductBatches",
                columns: new[] { "ProductId", "ExpDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_EntityKey_CreatedAt",
                table: "Alerts",
                columns: new[] { "EntityKey", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UiSections_IsVisible_DisplayOrder",
                table: "UiSections");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsActive_IsFeatured",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_ProductBatches_ProductId_ExpDate",
                table: "ProductBatches");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_EntityKey_CreatedAt",
                table: "Alerts");
        }
    }
}
