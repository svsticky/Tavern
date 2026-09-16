using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecificationQuestionCloseOnUnenrollmentDeadline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CloseAnswersOnUnenrollmentDeadline",
                table: "Activities");

            migrationBuilder.AddColumn<bool>(
                name: "CloseOnUnenrollmentDeadline",
                table: "SpecificationQuestions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CloseOnUnenrollmentDeadline",
                table: "SpecificationQuestions");

            migrationBuilder.AddColumn<bool>(
                name: "CloseAnswersOnUnenrollmentDeadline",
                table: "Activities",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
