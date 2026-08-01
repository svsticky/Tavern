using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class FixEnrollmentPaymentsMemberIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // InitialCreate applied a unique index to EnrollmentPayments.MemberId, inherited from
            // the base Payment entity's TPC-mapped MemberId uniqueness (only correct for
            // MembershipPayments, where a member has at most one). A member can have many
            // EnrollmentPayments (one per activity), so this constraint is wrong and blocks
            // legitimate inserts. It was never dropped by a later migration even though the
            // current model no longer declares it — the composite
            // IX_EnrollmentPayments_ActivityId_MemberId_PaidAt index (AddPerformanceIndexes)
            // already covers the query patterns that need it.
            migrationBuilder.DropIndex(
                name: "IX_EnrollmentPayments_MemberId",
                table: "EnrollmentPayments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentPayments_MemberId",
                table: "EnrollmentPayments",
                column: "MemberId",
                unique: true,
                filter: "\"MemberId\" IS NOT NULL");
        }
    }
}
