using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DKP.Infrastructure.Persistence.Migrations;

public partial class ManualDiscordForumProvisioning : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DiscordForumProvisionRequests",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                GuildId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                SectionKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                NewThreadId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                NewMessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
            }, constraints: table => table.PrimaryKey("PK_DiscordForumProvisionRequests", x => x.Id));
        migrationBuilder.CreateIndex("IX_DiscordForumProvisionRequests_GuildId_SectionKey", "DiscordForumProvisionRequests", new[] { "GuildId", "SectionKey" }, unique: true, filter: "\"Status\" IN ('Pending', 'Processing')");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("DiscordForumProvisionRequests");
}
