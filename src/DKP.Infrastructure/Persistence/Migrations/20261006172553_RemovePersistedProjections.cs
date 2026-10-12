using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DKP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemovePersistedProjections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DkpAwardPresetApplications");

            migrationBuilder.DropTable(
                name: "DkpBalanceProjections");

            migrationBuilder.DropTable(
                name: "LedgerEntries");

            migrationBuilder.DropTable(
                name: "ShopPurchaseProjections");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DkpAwardPresetApplications",
                columns: table => new
                {
                    DkpEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppliedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PresetId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
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
                    Action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ItemName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
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
                    CancellationEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ItemName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PurchaseEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    RollBonusValue = table.Column<int>(type: "integer", nullable: true),
                    ShopItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalDkpCost = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
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
                name: "IX_DkpBalanceProjections_LastEventId",
                table: "DkpBalanceProjections",
                column: "LastEventId");

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
        }
    }
}
