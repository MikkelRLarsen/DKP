using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DKP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DiscordRequestMessageDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeleteRequestedAtUtc",
                table: "DiscordNotificationOutbox",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "DiscordNotificationOutbox",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscordMessageId",
                table: "DiscordNotificationOutbox",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestId",
                table: "DiscordNotificationOutbox",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscordNotificationOutbox_RequestId",
                table: "DiscordNotificationOutbox",
                column: "RequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DiscordNotificationOutbox_RequestId",
                table: "DiscordNotificationOutbox");

            migrationBuilder.DropColumn(
                name: "DeleteRequestedAtUtc",
                table: "DiscordNotificationOutbox");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "DiscordNotificationOutbox");

            migrationBuilder.DropColumn(
                name: "DiscordMessageId",
                table: "DiscordNotificationOutbox");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "DiscordNotificationOutbox");
        }
    }
}
