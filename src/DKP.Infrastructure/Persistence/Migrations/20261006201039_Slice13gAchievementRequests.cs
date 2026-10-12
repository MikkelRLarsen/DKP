using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DKP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Slice13gAchievementRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "PresetId",
                table: "DkpAwardRequests",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "AchievementId",
                table: "DkpAwardRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DkpAwardRequests_AchievementId",
                table: "DkpAwardRequests",
                column: "AchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_DkpAwardRequests_UserId_AchievementId",
                table: "DkpAwardRequests",
                columns: new[] { "UserId", "AchievementId" },
                unique: true,
                filter: "\"Status\" = 'Pending'");

            migrationBuilder.AddForeignKey(
                name: "FK_DkpAwardRequests_AchievementDefinitions_AchievementId",
                table: "DkpAwardRequests",
                column: "AchievementId",
                principalTable: "AchievementDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DkpAwardRequests_AchievementDefinitions_AchievementId",
                table: "DkpAwardRequests");

            migrationBuilder.DropIndex(
                name: "IX_DkpAwardRequests_AchievementId",
                table: "DkpAwardRequests");

            migrationBuilder.DropIndex(
                name: "IX_DkpAwardRequests_UserId_AchievementId",
                table: "DkpAwardRequests");

            migrationBuilder.DropColumn(
                name: "AchievementId",
                table: "DkpAwardRequests");

            migrationBuilder.AlterColumn<Guid>(
                name: "PresetId",
                table: "DkpAwardRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
