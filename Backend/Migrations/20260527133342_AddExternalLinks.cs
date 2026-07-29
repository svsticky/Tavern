using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExternalLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TitleDutch = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TitleEnglish = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DescriptionDutch = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DescriptionEnglish = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IconPath = table.Column<string>(type: "text", nullable: true),
                    IconFileName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalLinks", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalLinks");
        }
    }
}
