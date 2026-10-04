using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DKP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShopAndDkpPresets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql("ALTER TABLE \"Users\" ADD COLUMN IF NOT EXISTS \"IsBlocked\" boolean NOT NULL DEFAULT FALSE; ALTER TABLE \"Users\" ADD COLUMN IF NOT EXISTS \"BlockedAtUtc\" timestamp with time zone; ALTER TABLE \"Users\" ADD COLUMN IF NOT EXISTS \"BlockedByUserId\" uuid; ALTER TABLE \"Users\" ADD COLUMN IF NOT EXISTS \"BlockReason\" character varying(500);");
			migrationBuilder.Sql("CREATE TABLE IF NOT EXISTS \"ShopItems\" (\"Id\" uuid NOT NULL, \"Key\" character varying(64) NOT NULL, \"Name\" character varying(128) NOT NULL, \"Description\" character varying(500) NOT NULL, \"Price\" integer NOT NULL, \"MaxPerUser\" integer NOT NULL, \"IsActive\" boolean NOT NULL, \"CreatedAtUtc\" timestamp with time zone NOT NULL, \"UpdatedAtUtc\" timestamp with time zone NOT NULL, CONSTRAINT \"PK_ShopItems\" PRIMARY KEY (\"Id\")); CREATE UNIQUE INDEX IF NOT EXISTS \"IX_ShopItems_Key\" ON \"ShopItems\" (\"Key\");");
			migrationBuilder.Sql("CREATE TABLE IF NOT EXISTS \"ShopPurchases\" (\"Id\" uuid NOT NULL, \"UserId\" uuid NOT NULL, \"ShopItemId\" uuid NOT NULL, \"Quantity\" integer NOT NULL, \"TotalDkpCost\" integer NOT NULL, \"CreatedByUserId\" uuid NOT NULL, \"CreatedAtUtc\" timestamp with time zone NOT NULL, \"CancelledAtUtc\" timestamp with time zone, CONSTRAINT \"PK_ShopPurchases\" PRIMARY KEY (\"Id\"), CONSTRAINT \"FK_ShopPurchases_Users_UserId\" FOREIGN KEY (\"UserId\") REFERENCES \"Users\" (\"Id\"), CONSTRAINT \"FK_ShopPurchases_ShopItems_ShopItemId\" FOREIGN KEY (\"ShopItemId\") REFERENCES \"ShopItems\" (\"Id\"), CONSTRAINT \"FK_ShopPurchases_Users_CreatedByUserId\" FOREIGN KEY (\"CreatedByUserId\") REFERENCES \"Users\" (\"Id\")); CREATE INDEX IF NOT EXISTS \"IX_ShopPurchases_UserId_ShopItemId\" ON \"ShopPurchases\" (\"UserId\", \"ShopItemId\");");
			migrationBuilder.Sql("CREATE TABLE IF NOT EXISTS \"DkpAwardPresets\" (\"Id\" uuid NOT NULL, \"Name\" character varying(128) NOT NULL, \"Amount\" integer NOT NULL, \"Reason\" character varying(500) NOT NULL, \"MaxApplicationsPerUser\" integer NOT NULL, \"IsActive\" boolean NOT NULL, \"CreatedAtUtc\" timestamp with time zone NOT NULL, \"UpdatedAtUtc\" timestamp with time zone NOT NULL, CONSTRAINT \"PK_DkpAwardPresets\" PRIMARY KEY (\"Id\")); CREATE UNIQUE INDEX IF NOT EXISTS \"IX_DkpAwardPresets_Name\" ON \"DkpAwardPresets\" (\"Name\");");
			migrationBuilder.Sql("CREATE TABLE IF NOT EXISTS \"DkpAwardPresetApplications\" (\"Id\" uuid NOT NULL, \"PresetId\" uuid NOT NULL, \"UserId\" uuid NOT NULL, \"DkpTransactionId\" uuid NOT NULL, \"AppliedByUserId\" uuid NOT NULL, \"CreatedAtUtc\" timestamp with time zone NOT NULL, CONSTRAINT \"PK_DkpAwardPresetApplications\" PRIMARY KEY (\"Id\"), CONSTRAINT \"FK_PresetApplications_Preset\" FOREIGN KEY (\"PresetId\") REFERENCES \"DkpAwardPresets\" (\"Id\"), CONSTRAINT \"FK_PresetApplications_User\" FOREIGN KEY (\"UserId\") REFERENCES \"Users\" (\"Id\"), CONSTRAINT \"FK_PresetApplications_Transaction\" FOREIGN KEY (\"DkpTransactionId\") REFERENCES \"DkpTransactions\" (\"Id\"), CONSTRAINT \"FK_PresetApplications_AppliedBy\" FOREIGN KEY (\"AppliedByUserId\") REFERENCES \"Users\" (\"Id\")); CREATE INDEX IF NOT EXISTS \"IX_PresetApplications_PresetId_UserId\" ON \"DkpAwardPresetApplications\" (\"PresetId\", \"UserId\");");
			migrationBuilder.Sql("INSERT INTO \"ShopItems\" (\"Id\",\"Key\",\"Name\",\"Description\",\"Price\",\"MaxPerUser\",\"IsActive\",\"CreatedAtUtc\",\"UpdatedAtUtc\") SELECT '00000000-0000-0000-0000-000000000008','soft-reserve','Soft Reserve','Soft Reserve slots',10,10,true,now(),now() WHERE NOT EXISTS (SELECT 1 FROM \"ShopItems\" WHERE \"Key\"='soft-reserve');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("DROP TABLE IF EXISTS \"DkpAwardPresetApplications\"; DROP TABLE IF EXISTS \"DkpAwardPresets\"; DROP TABLE IF EXISTS \"ShopPurchases\"; DROP TABLE IF EXISTS \"ShopItems\"; ALTER TABLE \"Users\" DROP COLUMN IF EXISTS \"BlockReason\"; ALTER TABLE \"Users\" DROP COLUMN IF EXISTS \"BlockedByUserId\"; ALTER TABLE \"Users\" DROP COLUMN IF EXISTS \"BlockedAtUtc\"; ALTER TABLE \"Users\" DROP COLUMN IF EXISTS \"IsBlocked\";");
        }
    }
}
