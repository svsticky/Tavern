using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationDocumentsAndMultilingualAnnouncements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Announcements",
                newName: "TitleEnglish");

            migrationBuilder.RenameColumn(
                name: "Content",
                table: "Announcements",
                newName: "ContentEnglish");

            migrationBuilder.AddColumn<string>(
                name: "ContentDutch",
                table: "Announcements",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TitleDutch",
                table: "Announcements",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "RegistrationDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NameDutch = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NameEnglish = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationDocuments", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrationDocuments");

            migrationBuilder.DropColumn(
                name: "ContentDutch",
                table: "Announcements");

            migrationBuilder.DropColumn(
                name: "TitleDutch",
                table: "Announcements");

            migrationBuilder.RenameColumn(
                name: "TitleEnglish",
                table: "Announcements",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "ContentEnglish",
                table: "Announcements",
                newName: "Content");
        }
    }
}
