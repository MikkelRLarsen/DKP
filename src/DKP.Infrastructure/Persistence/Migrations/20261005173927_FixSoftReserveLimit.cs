using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DKP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixSoftReserveLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql("UPDATE \"ShopItems\" SET \"MaxPerUser\" = 2 WHERE \"Key\" = 'soft-reserve';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql("UPDATE \"ShopItems\" SET \"MaxPerUser\" = 10 WHERE \"Key\" = 'soft-reserve';");
        }
    }
}
