using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class MailSubscriptionOutboxWorker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MollieFeePayments_MemberId",
                table: "MollieFeePayments");

            migrationBuilder.DropIndex(
                name: "IX_MembershipPayments_MemberId",
                table: "MembershipPayments");

            migrationBuilder.DropIndex(
                name: "IX_EnrollmentPayments_MemberId",
                table: "EnrollmentPayments");

            migrationBuilder.CreateTable(
                name: "MailSubscriptionOutboxTasks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    MailSubscription = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailSubscriptionOutboxTasks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MollieFeePayments_MemberId",
                table: "MollieFeePayments",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MailSubscriptionOutboxTasks");

            migrationBuilder.DropIndex(
                name: "IX_MollieFeePayments_MemberId",
                table: "MollieFeePayments");

            migrationBuilder.DropIndex(
                name: "IX_MembershipPayments_MemberId",
                table: "MembershipPayments");

            migrationBuilder.DropIndex(
                name: "IX_EnrollmentPayments_MemberId",
                table: "EnrollmentPayments");

            migrationBuilder.CreateIndex(
                name: "IX_MollieFeePayments_MemberId",
                table: "MollieFeePayments",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPayments_MemberId",
                table: "MembershipPayments",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentPayments_MemberId",
                table: "EnrollmentPayments",
                column: "MemberId");
        }
    }
}
