using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddEnrollmentPaymentsActivityMemberIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EnrollmentPayments_ActivityId",
                table: "EnrollmentPayments");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentPayments_ActivityId_MemberId",
                table: "EnrollmentPayments",
                columns: new[] { "ActivityId", "MemberId" },
                filter: "\"PaidAt\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EnrollmentPayments_ActivityId_MemberId",
                table: "EnrollmentPayments");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentPayments_ActivityId",
                table: "EnrollmentPayments",
                column: "ActivityId");
        }
    }
}
