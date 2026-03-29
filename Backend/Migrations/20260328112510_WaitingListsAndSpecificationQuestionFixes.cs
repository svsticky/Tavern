using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class WaitingListsAndSpecificationQuestionFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnrollmentId",
                table: "SpecificationAnswers");

            migrationBuilder.AddColumn<string>(
                name: "Options",
                table: "SpecificationQuestions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MemberId",
                table: "SpecificationAnswers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Members",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnWaitingList",
                table: "Enrollments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredOn",
                table: "Enrollments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationAnswers_MemberId",
                table: "SpecificationAnswers",
                column: "MemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_SpecificationAnswers_Members_MemberId",
                table: "SpecificationAnswers",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SpecificationAnswers_Members_MemberId",
                table: "SpecificationAnswers");

            migrationBuilder.DropIndex(
                name: "IX_SpecificationAnswers_MemberId",
                table: "SpecificationAnswers");

            migrationBuilder.DropColumn(
                name: "Options",
                table: "SpecificationQuestions");

            migrationBuilder.DropColumn(
                name: "MemberId",
                table: "SpecificationAnswers");

            migrationBuilder.DropColumn(
                name: "IsOnWaitingList",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "RegisteredOn",
                table: "Enrollments");

            migrationBuilder.AddColumn<long>(
                name: "EnrollmentId",
                table: "SpecificationAnswers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Members",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
