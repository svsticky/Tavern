using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class FixPaymentMemberIdUniqueIndexScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentServiceFeePayments_MemberId",
                table: "PaymentServiceFeePayments");

            migrationBuilder.DropIndex(
                name: "IX_MembershipPayments_MemberId",
                table: "MembershipPayments");

            migrationBuilder.DropIndex(
                name: "IX_EnrollmentPayments_MemberId",
                table: "EnrollmentPayments");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentServiceFeePayments_MemberId",
                table: "PaymentServiceFeePayments",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPayments_MemberId",
                table: "MembershipPayments",
                column: "MemberId",
                unique: true,
                filter: "\"MemberId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentPayments_MemberId",
                table: "EnrollmentPayments",
                column: "MemberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentServiceFeePayments_MemberId",
                table: "PaymentServiceFeePayments");

            migrationBuilder.DropIndex(
                name: "IX_MembershipPayments_MemberId",
                table: "MembershipPayments");

            migrationBuilder.DropIndex(
                name: "IX_EnrollmentPayments_MemberId",
                table: "EnrollmentPayments");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentServiceFeePayments_MemberId",
                table: "PaymentServiceFeePayments",
                column: "MemberId",
                unique: true,
                filter: "\"MemberId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPayments_MemberId",
                table: "MembershipPayments",
                column: "MemberId",
                unique: true,
                filter: "\"MemberId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentPayments_MemberId",
                table: "EnrollmentPayments",
                column: "MemberId",
                unique: true,
                filter: "\"MemberId\" IS NOT NULL");
        }
    }
}
