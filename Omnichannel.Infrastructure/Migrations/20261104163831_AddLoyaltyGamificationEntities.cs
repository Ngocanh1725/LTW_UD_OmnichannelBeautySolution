using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Omnichannel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLoyaltyGamificationEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentStreak",
                table: "LoyaltyWallets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCheckInDate",
                table: "LoyaltyWallets",
                type: "datetime2(7)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferralCode",
                table: "LoyaltyWallets",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LoyaltyCheckIns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    WalletId = table.Column<int>(type: "int", nullable: false),
                    CheckInDate = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    StreakDay = table.Column<int>(type: "int", nullable: false),
                    PointsAwarded = table.Column<int>(type: "int", nullable: false),
                    PointTransactionId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyCheckIns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoyaltyCheckIns_LoyaltyWallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "LoyaltyWallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoyaltyCheckIns_PointTransactions_PointTransactionId",
                        column: x => x.PointTransactionId,
                        principalTable: "PointTransactions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LoyaltyCheckIns_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyReferrals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferrerUserId = table.Column<int>(type: "int", nullable: false),
                    ReferredUserId = table.Column<int>(type: "int", nullable: false),
                    ReferralCode = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    ReferrerPointsAwarded = table.Column<int>(type: "int", nullable: false),
                    ReferredPointsAwarded = table.Column<int>(type: "int", nullable: false),
                    RewardVoucherCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    CompletedOrderId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    RewardedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyReferrals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoyaltyReferrals_Orders_CompletedOrderId",
                        column: x => x.CompletedOrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LoyaltyReferrals_Users_ReferredUserId",
                        column: x => x.ReferredUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoyaltyReferrals_Users_ReferrerUserId",
                        column: x => x.ReferrerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyWallets_ReferralCode",
                table: "LoyaltyWallets",
                column: "ReferralCode",
                unique: true,
                filter: "[ReferralCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyCheckIns_PointTransactionId",
                table: "LoyaltyCheckIns",
                column: "PointTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyCheckIns_UserId_CheckInDate",
                table: "LoyaltyCheckIns",
                columns: new[] { "UserId", "CheckInDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyCheckIns_WalletId",
                table: "LoyaltyCheckIns",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyReferrals_CompletedOrderId",
                table: "LoyaltyReferrals",
                column: "CompletedOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyReferrals_ReferredUserId",
                table: "LoyaltyReferrals",
                column: "ReferredUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyReferrals_ReferrerUserId_Status",
                table: "LoyaltyReferrals",
                columns: new[] { "ReferrerUserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoyaltyCheckIns");

            migrationBuilder.DropTable(
                name: "LoyaltyReferrals");

            migrationBuilder.DropIndex(
                name: "IX_LoyaltyWallets_ReferralCode",
                table: "LoyaltyWallets");

            migrationBuilder.DropColumn(
                name: "CurrentStreak",
                table: "LoyaltyWallets");

            migrationBuilder.DropColumn(
                name: "LastCheckInDate",
                table: "LoyaltyWallets");

            migrationBuilder.DropColumn(
                name: "ReferralCode",
                table: "LoyaltyWallets");
        }
    }
}

