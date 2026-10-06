using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DKP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Slice13cDkpAwardRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DkpAwardRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PresetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewComment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DkpEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DkpAwardRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DkpAwardRequests_DkpAwardPresets_PresetId",
                        column: x => x.PresetId,
                        principalTable: "DkpAwardPresets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DkpAwardRequests_DkpEvents_DkpEventId",
                        column: x => x.DkpEventId,
                        principalTable: "DkpEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DkpAwardRequests_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DkpAwardRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DkpAwardRequests_DkpEventId",
                table: "DkpAwardRequests",
                column: "DkpEventId");

            migrationBuilder.CreateIndex(
                name: "IX_DkpAwardRequests_PresetId",
                table: "DkpAwardRequests",
                column: "PresetId");

            migrationBuilder.CreateIndex(
                name: "IX_DkpAwardRequests_ReviewedByUserId",
                table: "DkpAwardRequests",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DkpAwardRequests_UserId_PresetId",
                table: "DkpAwardRequests",
                columns: new[] { "UserId", "PresetId" },
                unique: true,
                filter: "\"Status\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_DkpAwardRequests_UserId_PresetId_Status",
                table: "DkpAwardRequests",
                columns: new[] { "UserId", "PresetId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DkpAwardRequests");
        }
    }
}
