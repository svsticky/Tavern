using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddBegunstigerPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BegunstigerPayments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "nextval('\"PaymentSequence\"')"),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    PaymentServiceId = table.Column<string>(type: "text", nullable: false),
                    PaymentIntentUrl = table.Column<string>(type: "text", nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    AccountingToolEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    ManuallyMarkedAsPaid = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BegunstigerPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BegunstigerPayments_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BegunstigerPayments_MemberId",
                table: "BegunstigerPayments",
                column: "MemberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BegunstigerPayments");
        }
    }
}
