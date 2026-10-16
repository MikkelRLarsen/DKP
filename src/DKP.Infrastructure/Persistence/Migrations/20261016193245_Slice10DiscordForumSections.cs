using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DKP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Slice10DiscordForumSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscordForumSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SectionKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ForumChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    ThreadId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    InitialMessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    InitialMessageContent = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordForumSections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscordForumSections_GuildId_SectionKey",
                table: "DiscordForumSections",
                columns: new[] { "GuildId", "SectionKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscordForumSections");
        }
    }
}
