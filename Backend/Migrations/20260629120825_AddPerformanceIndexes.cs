using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GroupMemberships_MemberId",
                table: "GroupMemberships");

            migrationBuilder.DropIndex(
                name: "IX_EnrollmentPayments_ActivityId",
                table: "EnrollmentPayments");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMemberships_MemberId_MembershipYear",
                table: "GroupMemberships",
                columns: new[] { "MemberId", "MembershipYear" });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_ActivityId",
                table: "Enrollments",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentPayments_ActivityId_MemberId_PaidAt",
                table: "EnrollmentPayments",
                columns: new[] { "ActivityId", "MemberId" },
                filter: "\"PaidAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_DateTimeStart",
                table: "Activities",
                column: "DateTimeStart");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GroupMemberships_MemberId_MembershipYear",
                table: "GroupMemberships");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_ActivityId",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_EnrollmentPayments_ActivityId_MemberId_PaidAt",
                table: "EnrollmentPayments");

            migrationBuilder.DropIndex(
                name: "IX_Activities_DateTimeStart",
                table: "Activities");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMemberships_MemberId",
                table: "GroupMemberships",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentPayments_ActivityId",
                table: "EnrollmentPayments",
                column: "ActivityId");
        }
    }
}
