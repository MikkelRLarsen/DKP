using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DKP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ShopPurchaseLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseLimitId",
                table: "ShopItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ShopPurchaseLimits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SharedKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MaxPerUser = table.Column<int>(type: "integer", nullable: true),
                    MaxActivePerUser = table.Column<int>(type: "integer", nullable: true),
                    MaxPerDay = table.Column<int>(type: "integer", nullable: true),
                    MaxPerWeek = table.Column<int>(type: "integer", nullable: true),
                    MaxPerMonth = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopPurchaseLimits", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000008"),
                column: "PurchaseLimitId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000010"),
                column: "PurchaseLimitId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000020"),
                column: "PurchaseLimitId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000030"),
                column: "PurchaseLimitId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000040"),
                column: "PurchaseLimitId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_ShopItems_PurchaseLimitId",
                table: "ShopItems",
                column: "PurchaseLimitId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopPurchaseLimits_SharedKey",
                table: "ShopPurchaseLimits",
                column: "SharedKey",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ShopItems_ShopPurchaseLimits_PurchaseLimitId",
                table: "ShopItems",
                column: "PurchaseLimitId",
                principalTable: "ShopPurchaseLimits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShopItems_ShopPurchaseLimits_PurchaseLimitId",
                table: "ShopItems");

            migrationBuilder.DropTable(
                name: "ShopPurchaseLimits");

            migrationBuilder.DropIndex(
                name: "IX_ShopItems_PurchaseLimitId",
                table: "ShopItems");

            migrationBuilder.DropColumn(
                name: "PurchaseLimitId",
                table: "ShopItems");
        }
    }
}
