using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DKP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CharacterFirstAndLastName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Characters_UserId_Name_Realm",
                table: "Characters");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Characters",
                newName: "FirstName");

            migrationBuilder.RenameColumn(
                name: "Realm",
                table: "Characters",
                newName: "LastName");

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Characters",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(24)",
                oldMaxLength: 24);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Characters",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.CreateIndex(
                name: "IX_Characters_UserId_FirstName_LastName",
                table: "Characters",
                columns: new[] { "UserId", "FirstName", "LastName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Characters_UserId_FirstName_LastName",
                table: "Characters");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "Characters",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "Characters",
                newName: "Realm");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Characters",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.CreateIndex(
                name: "IX_Characters_UserId_Name_Realm",
                table: "Characters",
                columns: new[] { "UserId", "Name", "Realm" },
                unique: true);
        }
    }
}
