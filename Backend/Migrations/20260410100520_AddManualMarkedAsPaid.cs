using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddManualMarkedAsPaid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ManuallyMarkedAsPaid",
                table: "MembershipPayments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ManuallyMarkedAsPaid",
                table: "EnrollmentPayments",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ManuallyMarkedAsPaid",
                table: "MembershipPayments");

            migrationBuilder.DropColumn(
                name: "ManuallyMarkedAsPaid",
                table: "EnrollmentPayments");
        }
    }
}
