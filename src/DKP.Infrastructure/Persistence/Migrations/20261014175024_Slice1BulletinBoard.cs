using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DKP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Slice1BulletinBoard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BulletinItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DkpAmount = table.Column<int>(type: "integer", nullable: false),
                    MaxCompletions = table.Column<int>(type: "integer", nullable: false),
                    UniquePerUser = table.Column<bool>(type: "boolean", nullable: false),
                    DeadlineUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulletinItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BulletinClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BulletinItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewComment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DkpEventIdsJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulletinClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BulletinClaims_BulletinItems_BulletinItemId",
                        column: x => x.BulletinItemId,
                        principalTable: "BulletinItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BulletinClaims_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BulletinClaims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BulletinClaims_BulletinItemId_Status",
                table: "BulletinClaims",
                columns: new[] { "BulletinItemId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BulletinClaims_ReviewedByUserId",
                table: "BulletinClaims",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BulletinClaims_UserId_BulletinItemId_Status",
                table: "BulletinClaims",
                columns: new[] { "UserId", "BulletinItemId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BulletinItems_IsActive_DeadlineUtc",
                table: "BulletinItems",
                columns: new[] { "IsActive", "DeadlineUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BulletinClaims");

            migrationBuilder.DropTable(
                name: "BulletinItems");
        }
    }
}
