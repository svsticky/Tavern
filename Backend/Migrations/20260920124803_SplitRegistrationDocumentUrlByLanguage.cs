using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class SplitRegistrationDocumentUrlByLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Url",
                table: "RegistrationDocuments",
                newName: "UrlEnglish");

            migrationBuilder.AddColumn<string>(
                name: "UrlDutch",
                table: "RegistrationDocuments",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            // Existing documents have a single URL; use it for both languages.
            migrationBuilder.Sql("UPDATE \"RegistrationDocuments\" SET \"UrlDutch\" = \"UrlEnglish\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UrlDutch",
                table: "RegistrationDocuments");

            migrationBuilder.RenameColumn(
                name: "UrlEnglish",
                table: "RegistrationDocuments",
                newName: "Url");
        }
    }
}
