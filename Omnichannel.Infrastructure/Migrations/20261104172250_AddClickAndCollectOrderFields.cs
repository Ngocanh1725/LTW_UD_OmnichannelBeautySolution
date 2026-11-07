using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Omnichannel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClickAndCollectOrderFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LoyaltyCheckIns_PointTransactions_PointTransactionId",
                table: "LoyaltyCheckIns");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryMethod",
                table: "Orders",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "HOME_DELIVERY");

            migrationBuilder.AddColumn<DateTime>(
                name: "PickedUpAt",
                table: "Orders",
                type: "datetime2(7)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickupCode",
                table: "Orders",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PickupExpiresAt",
                table: "Orders",
                type: "datetime2(7)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PickupStoreId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PickupCode",
                table: "Orders",
                column: "PickupCode",
                filter: "[PickupCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PickupStore_Status",
                table: "Orders",
                columns: new[] { "DeliveryMethod", "PickupStoreId", "OrderStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PickupStoreId",
                table: "Orders",
                column: "PickupStoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_LoyaltyCheckIns_PointTransactions_PointTransactionId",
                table: "LoyaltyCheckIns",
                column: "PointTransactionId",
                principalTable: "PointTransactions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Stores_PickupStoreId",
                table: "Orders",
                column: "PickupStoreId",
                principalTable: "Stores",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LoyaltyCheckIns_PointTransactions_PointTransactionId",
                table: "LoyaltyCheckIns");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Stores_PickupStoreId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PickupCode",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PickupStore_Status",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PickupStoreId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryMethod",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PickedUpAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PickupCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PickupExpiresAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PickupStoreId",
                table: "Orders");

            migrationBuilder.AddForeignKey(
                name: "FK_LoyaltyCheckIns_PointTransactions_PointTransactionId",
                table: "LoyaltyCheckIns",
                column: "PointTransactionId",
                principalTable: "PointTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
