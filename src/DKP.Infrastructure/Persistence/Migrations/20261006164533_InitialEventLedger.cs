using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DKP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialEventLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DkpAwardPresets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MaxApplicationsPerUser = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DkpAwardPresets", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "ShopItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Price = table.Column<int>(type: "integer", nullable: false),
                    MaxPerUser = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RollBonusValue = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscordId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DiscordName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AvatarUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    BlockedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BlockedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    BlockReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LastName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsMain = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Characters_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DkpEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    EventType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DkpEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DkpEvents_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DkpEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DkpAwardPresetApplications",
                columns: table => new
                {
                    DkpEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    PresetId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppliedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DkpAwardPresetApplications", x => x.DkpEventId);
                    table.ForeignKey(
                        name: "FK_DkpAwardPresetApplications_DkpAwardPresets_PresetId",
                        column: x => x.PresetId,
                        principalTable: "DkpAwardPresets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DkpAwardPresetApplications_DkpEvents_DkpEventId",
                        column: x => x.DkpEventId,
                        principalTable: "DkpEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DkpAwardPresetApplications_Users_AppliedByUserId",
                        column: x => x.AppliedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DkpAwardPresetApplications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DkpBalanceProjections",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Balance = table.Column<int>(type: "integer", nullable: false),
                    LastEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DkpBalanceProjections", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_DkpBalanceProjections_DkpEvents_LastEventId",
                        column: x => x.LastEventId,
                        principalTable: "DkpEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DkpBalanceProjections_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LedgerEntries",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ItemName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LedgerEntries", x => x.EventId);
                    table.ForeignKey(
                        name: "FK_LedgerEntries_DkpEvents_EventId",
                        column: x => x.EventId,
                        principalTable: "DkpEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LedgerEntries_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LedgerEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShopPurchaseProjections",
                columns: table => new
                {
                    PurchaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShopItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ItemName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<int>(type: "integer", nullable: false),
                    TotalDkpCost = table.Column<int>(type: "integer", nullable: false),
                    RollBonusValue = table.Column<int>(type: "integer", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationEventId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopPurchaseProjections", x => x.PurchaseId);
                    table.ForeignKey(
                        name: "FK_ShopPurchaseProjections_DkpEvents_CancellationEventId",
                        column: x => x.CancellationEventId,
                        principalTable: "DkpEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShopPurchaseProjections_DkpEvents_PurchaseEventId",
                        column: x => x.PurchaseEventId,
                        principalTable: "DkpEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShopPurchaseProjections_ShopItems_ShopItemId",
                        column: x => x.ShopItemId,
                        principalTable: "ShopItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShopPurchaseProjections_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShopPurchaseProjections_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "GuildSettings",
                columns: new[] { "Id", "DefaultReserveLimit" },
                values: new object[] { 1, 0 });

            migrationBuilder.InsertData(
                table: "ShopItems",
                columns: new[] { "Id", "CreatedAtUtc", "Description", "IsActive", "Key", "MaxPerUser", "Name", "Price", "RollBonusValue", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000008"), new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Additional Soft Reserve", true, "soft-reserve", 2, "Soft Reserve", 10, null, new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000010"), new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Roll bonus +10", true, "roll-bonus-10", 1, "RollBonus 10", 10, 10, new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000020"), new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Roll bonus +20", true, "roll-bonus-20", 1, "RollBonus 20", 30, 20, new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000030"), new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Roll bonus +30", true, "roll-bonus-30", 1, "RollBonus 30", 60, 30, new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000040"), new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Roll bonus +40", true, "roll-bonus-40", 1, "RollBonus 40", 120, 40, new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_UserId_FirstName_LastName",
                table: "Characters",
                columns: new[] { "UserId", "FirstName", "LastName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Characters_UserId_IsMain",
                table: "Characters",
                columns: new[] { "UserId", "IsMain" },
                unique: true,
                filter: "\"IsMain\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_DkpAwardPresetApplications_AppliedByUserId",
                table: "DkpAwardPresetApplications",
                column: "AppliedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DkpAwardPresetApplications_PresetId_UserId",
                table: "DkpAwardPresetApplications",
                columns: new[] { "PresetId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_DkpAwardPresetApplications_UserId",
                table: "DkpAwardPresetApplications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DkpAwardPresets_Name",
                table: "DkpAwardPresets",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DkpBalanceProjections_LastEventId",
                table: "DkpBalanceProjections",
                column: "LastEventId");

            migrationBuilder.CreateIndex(
                name: "IX_DkpEvents_ActorUserId",
                table: "DkpEvents",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DkpEvents_AggregateType_AggregateId_Sequence",
                table: "DkpEvents",
                columns: new[] { "AggregateType", "AggregateId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DkpEvents_CorrelationId",
                table: "DkpEvents",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_DkpEvents_UserId_OccurredAtUtc",
                table: "DkpEvents",
                columns: new[] { "UserId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_Action_CreatedAtUtc",
                table: "LedgerEntries",
                columns: new[] { "Action", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_ActorUserId",
                table: "LedgerEntries",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_CreatedAtUtc_EventId",
                table: "LedgerEntries",
                columns: new[] { "CreatedAtUtc", "EventId" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_UserId_Sequence",
                table: "LedgerEntries",
                columns: new[] { "UserId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopItems_Key",
                table: "ShopItems",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopPurchaseProjections_CancellationEventId",
                table: "ShopPurchaseProjections",
                column: "CancellationEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopPurchaseProjections_CreatedByUserId",
                table: "ShopPurchaseProjections",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopPurchaseProjections_PurchaseEventId",
                table: "ShopPurchaseProjections",
                column: "PurchaseEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopPurchaseProjections_ShopItemId",
                table: "ShopPurchaseProjections",
                column: "ShopItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopPurchaseProjections_UserId_ShopItemId_CancelledAtUtc",
                table: "ShopPurchaseProjections",
                columns: new[] { "UserId", "ShopItemId", "CancelledAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_DiscordId",
                table: "Users",
                column: "DiscordId",
                unique: true);

            // Defense in depth: set-based EF updates/deletes must not bypass append-only storage.
            migrationBuilder.Sql("""
                CREATE FUNCTION reject_ledger_mutation() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION 'Ledger events are append-only';
                END;
                $$;
                CREATE TRIGGER dkp_events_append_only BEFORE UPDATE OR DELETE ON "DkpEvents"
                FOR EACH ROW EXECUTE FUNCTION reject_ledger_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "DkpAwardPresetApplications");

            migrationBuilder.DropTable(
                name: "DkpBalanceProjections");

            migrationBuilder.DropTable(
                name: "GuildSettings");

            migrationBuilder.DropTable(
                name: "LedgerEntries");

            migrationBuilder.DropTable(
                name: "ShopPurchaseProjections");

            migrationBuilder.DropTable(
                name: "DkpAwardPresets");

            migrationBuilder.DropTable(
                name: "DkpEvents");

            migrationBuilder.Sql("DROP FUNCTION reject_ledger_mutation();");

            migrationBuilder.DropTable(
                name: "ShopItems");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
