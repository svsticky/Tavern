using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityCloseAnswersOnUnenrollmentDeadline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnswerDeadline",
                table: "SpecificationQuestions");

            migrationBuilder.AddColumn<bool>(
                name: "CloseAnswersOnUnenrollmentDeadline",
                table: "Activities",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CloseAnswersOnUnenrollmentDeadline",
                table: "Activities");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AnswerDeadline",
                table: "SpecificationQuestions",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
