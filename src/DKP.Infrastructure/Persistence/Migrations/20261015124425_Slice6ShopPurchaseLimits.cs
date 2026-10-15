using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DKP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Slice6ShopPurchaseLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxActivePerUser",
                table: "ShopItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxPerDay",
                table: "ShopItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxPerMonth",
                table: "ShopItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxPerWeek",
                table: "ShopItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurchaseLimitKey",
                table: "ShopItems",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000008"),
                columns: new[] { "MaxActivePerUser", "MaxPerDay", "MaxPerMonth", "MaxPerWeek", "PurchaseLimitKey" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000010"),
                columns: new[] { "MaxActivePerUser", "MaxPerDay", "MaxPerMonth", "MaxPerWeek", "PurchaseLimitKey" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000020"),
                columns: new[] { "MaxActivePerUser", "MaxPerDay", "MaxPerMonth", "MaxPerWeek", "PurchaseLimitKey" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000030"),
                columns: new[] { "MaxActivePerUser", "MaxPerDay", "MaxPerMonth", "MaxPerWeek", "PurchaseLimitKey" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "ShopItems",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000040"),
                columns: new[] { "MaxActivePerUser", "MaxPerDay", "MaxPerMonth", "MaxPerWeek", "PurchaseLimitKey" },
                values: new object[] { null, null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxActivePerUser",
                table: "ShopItems");

            migrationBuilder.DropColumn(
                name: "MaxPerDay",
                table: "ShopItems");

            migrationBuilder.DropColumn(
                name: "MaxPerMonth",
                table: "ShopItems");

            migrationBuilder.DropColumn(
                name: "MaxPerWeek",
                table: "ShopItems");

            migrationBuilder.DropColumn(
                name: "PurchaseLimitKey",
                table: "ShopItems");
        }
    }
}
