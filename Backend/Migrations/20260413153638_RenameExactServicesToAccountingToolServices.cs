using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class RenameExactServicesToAccountingToolServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExactOutboxTasks");

            migrationBuilder.RenameColumn(
                name: "ExactEntryId",
                table: "MembershipPayments",
                newName: "AccountingToolEntryId");

            migrationBuilder.RenameColumn(
                name: "ExactEntryId",
                table: "EnrollmentPayments",
                newName: "AccountingToolEntryId");

            migrationBuilder.CreateTable(
                name: "AccountingToolOutboxTasks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PaymentId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    TaskType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingToolOutboxTasks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingToolOutboxTasks_PaymentId",
                table: "AccountingToolOutboxTasks",
                column: "PaymentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountingToolOutboxTasks");

            migrationBuilder.RenameColumn(
                name: "AccountingToolEntryId",
                table: "MembershipPayments",
                newName: "ExactEntryId");

            migrationBuilder.RenameColumn(
                name: "AccountingToolEntryId",
                table: "EnrollmentPayments",
                newName: "ExactEntryId");

            migrationBuilder.CreateTable(
                name: "ExactOutboxTasks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PaymentId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    TaskType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExactOutboxTasks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExactOutboxTasks_PaymentId",
                table: "ExactOutboxTasks",
                column: "PaymentId");
        }
    }
}
