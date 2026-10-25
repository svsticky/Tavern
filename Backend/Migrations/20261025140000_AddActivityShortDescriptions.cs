using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityShortDescriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "EnglishDescription",
                table: "Activities",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "DutchDescription",
                table: "Activities",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AddColumn<string>(
                name: "ShortDutchDescription",
                table: "Activities",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShortEnglishDescription",
                table: "Activities",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE \"Activities\" SET \"ShortDutchDescription\" = SUBSTRING(\"DutchDescription\", 1, 250) WHERE \"ShortDutchDescription\" = '' AND \"DutchDescription\" IS NOT NULL;");
            migrationBuilder.Sql("UPDATE \"Activities\" SET \"ShortEnglishDescription\" = SUBSTRING(\"EnglishDescription\", 1, 250) WHERE \"ShortEnglishDescription\" = '' AND \"EnglishDescription\" IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShortDutchDescription",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "ShortEnglishDescription",
                table: "Activities");

            migrationBuilder.AlterColumn<string>(
                name: "EnglishDescription",
                table: "Activities",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DutchDescription",
                table: "Activities",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);
        }
    }
}
