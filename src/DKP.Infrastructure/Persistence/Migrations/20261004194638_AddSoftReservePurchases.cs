using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DKP.Infrastructure.Persistence.Migrations;

public partial class AddSoftReservePurchases : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.CreateTable(
			name: "SoftReservePurchases",
			columns: table => new
			{
				Id = table.Column<Guid>(type: "uuid", nullable: false),
				UserId = table.Column<Guid>(type: "uuid", nullable: false),
				ReserveNumber = table.Column<int>(type: "integer", nullable: false),
				DkpCost = table.Column<int>(type: "integer", nullable: false),
				CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_SoftReservePurchases", x => x.Id);
				table.ForeignKey("FK_SoftReservePurchases_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateIndex("IX_SoftReservePurchases_UserId", "SoftReservePurchases", "UserId");
		migrationBuilder.CreateIndex("IX_SoftReservePurchases_UserId_ReserveNumber", "SoftReservePurchases", new[] { "UserId", "ReserveNumber" }, unique: true);
	}

	protected override void Down(MigrationBuilder migrationBuilder)
		=> migrationBuilder.DropTable(name: "SoftReservePurchases");
}
