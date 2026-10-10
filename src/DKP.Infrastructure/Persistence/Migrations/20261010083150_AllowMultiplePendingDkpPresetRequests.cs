using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DKP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultiplePendingDkpPresetRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DkpAwardRequests_UserId_PresetId",
                table: "DkpAwardRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DkpAwardRequests_UserId_PresetId",
                table: "DkpAwardRequests",
                columns: new[] { "UserId", "PresetId" },
                unique: true,
                filter: "\"Status\" = 'Pending'");
        }
    }
}
