using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class PaidAtToDateTimeOffsetType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"MembershipPayments\" ALTER COLUMN \"PaidAt\" TYPE timestamp with time zone USING \"PaidAt\"::timestamp with time zone;");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "PaidAt",
                table: "MembershipPayments",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.Sql("ALTER TABLE \"EnrollmentPayments\" ALTER COLUMN \"PaidAt\" TYPE timestamp with time zone USING \"PaidAt\"::timestamp with time zone;");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "PaidAt",
                table: "EnrollmentPayments",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PaidAt",
                table: "MembershipPayments",
                type: "text",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaidAt",
                table: "EnrollmentPayments",
                type: "text",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
