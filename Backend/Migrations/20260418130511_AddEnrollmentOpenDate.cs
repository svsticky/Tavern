using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddEnrollmentOpenDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Members_MemberId",
                table: "Enrollments");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EnrollOpenDate",
                table: "Activities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Members_MemberId",
                table: "Enrollments",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Members_MemberId",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "EnrollOpenDate",
                table: "Activities");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Members_MemberId",
                table: "Enrollments",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
