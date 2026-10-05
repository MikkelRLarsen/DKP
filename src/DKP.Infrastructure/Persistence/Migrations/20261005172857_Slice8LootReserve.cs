using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DKP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Slice8LootReserve : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RollBonus",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RollBonusValue",
                table: "ShopItems",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GuildSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DefaultReserveLimit = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildSettings", x => x.Id);
                });

            migrationBuilder.Sql("INSERT INTO \"GuildSettings\" (\"Id\", \"DefaultReserveLimit\") VALUES (1, 0) ON CONFLICT (\"Id\") DO NOTHING;");
            migrationBuilder.Sql("INSERT INTO \"ShopItems\" (\"Id\",\"Key\",\"Name\",\"Description\",\"Price\",\"MaxPerUser\",\"IsActive\",\"CreatedAtUtc\",\"UpdatedAtUtc\",\"RollBonusValue\") VALUES ('00000000-0000-0000-0000-000000000010','roll-bonus-10','Roll Bonus 10','Adds 10 RollBonus to the LootReserve export.',10,1,true,now(),now(),10),('00000000-0000-0000-0000-000000000020','roll-bonus-20','Roll Bonus 20','Adds 20 RollBonus to the LootReserve export.',30,1,true,now(),now(),20),('00000000-0000-0000-0000-000000000030','roll-bonus-30','Roll Bonus 30','Adds 30 RollBonus to the LootReserve export.',60,1,true,now(),now(),30),('00000000-0000-0000-0000-000000000040','roll-bonus-40','Roll Bonus 40','Adds 40 RollBonus to the LootReserve export.',120,1,true,now(),now(),40) ON CONFLICT (\"Key\") DO NOTHING;");
            migrationBuilder.Sql("INSERT INTO \"ShopItems\" (\"Id\",\"Key\",\"Name\",\"Description\",\"Price\",\"MaxPerUser\",\"IsActive\",\"CreatedAtUtc\",\"UpdatedAtUtc\") SELECT '00000000-0000-0000-0000-000000000008','soft-reserve','Soft Reserve','Soft Reserve slots',10,10,true,now(),now() WHERE NOT EXISTS (SELECT 1 FROM \"ShopItems\" WHERE \"Key\"='soft-reserve');");
            migrationBuilder.Sql("INSERT INTO \"ShopPurchases\" (\"Id\",\"UserId\",\"ShopItemId\",\"Quantity\",\"TotalDkpCost\",\"CreatedByUserId\",\"CreatedAtUtc\",\"CancelledAtUtc\") SELECT old.\"Id\", old.\"UserId\", item.\"Id\", old.\"Quantity\", old.\"DkpCost\", old.\"UserId\", old.\"CreatedAtUtc\", old.\"CancelledAtUtc\" FROM \"SoftReservePurchases\" old CROSS JOIN \"ShopItems\" item WHERE item.\"Key\"='soft-reserve' AND NOT EXISTS (SELECT 1 FROM \"ShopPurchases\" existing WHERE existing.\"Id\"=old.\"Id\");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildSettings");

            migrationBuilder.DropColumn(
                name: "RollBonus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RollBonusValue",
                table: "ShopItems");
        }
    }
}
