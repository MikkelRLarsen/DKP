using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DKP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Slice13cRequestQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DkpEventIdsJson",
                table: "DkpAwardRequests",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "DkpAwardRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DkpEventIdsJson",
                table: "DkpAwardRequests");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "DkpAwardRequests");
        }
    }
}
