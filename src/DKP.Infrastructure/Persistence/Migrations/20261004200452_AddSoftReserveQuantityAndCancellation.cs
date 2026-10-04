using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DKP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftReserveQuantityAndCancellation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF to_regclass('"SoftReservePurchases"') IS NULL THEN
                        CREATE TABLE "SoftReservePurchases" (
                            "Id" uuid NOT NULL,
                            "UserId" uuid NOT NULL,
                            "Quantity" integer NOT NULL,
                            "DkpCost" integer NOT NULL,
                            "CreatedAtUtc" timestamp with time zone NOT NULL,
                            "CancelledAtUtc" timestamp with time zone NULL,
                            CONSTRAINT "PK_SoftReservePurchases" PRIMARY KEY ("Id"),
                            CONSTRAINT "FK_SoftReservePurchases_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
                        );
                    ELSE
                        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'SoftReservePurchases' AND column_name = 'ReserveNumber')
                           AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'SoftReservePurchases' AND column_name = 'Quantity') THEN
                            ALTER TABLE "SoftReservePurchases" RENAME COLUMN "ReserveNumber" TO "Quantity";
                        END IF;
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'SoftReservePurchases' AND column_name = 'CancelledAtUtc') THEN
                            ALTER TABLE "SoftReservePurchases" ADD COLUMN "CancelledAtUtc" timestamp with time zone NULL;
                        END IF;
                    END IF;
                END $$;
                DROP INDEX IF EXISTS "IX_SoftReservePurchases_UserId_ReserveNumber";
                CREATE INDEX IF NOT EXISTS "IX_SoftReservePurchases_UserId" ON "SoftReservePurchases" ("UserId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"SoftReservePurchases\";");
        }
    }
}
