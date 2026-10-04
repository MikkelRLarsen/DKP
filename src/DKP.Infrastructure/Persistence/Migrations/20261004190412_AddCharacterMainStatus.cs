using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DKP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterMainStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMain",
                table: "Characters",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Characters_UserId_IsMain",
                table: "Characters",
                columns: new[] { "UserId", "IsMain" },
                unique: true,
                filter: "\"IsMain\" = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Characters_UserId_IsMain",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "IsMain",
                table: "Characters");
        }
    }
}
