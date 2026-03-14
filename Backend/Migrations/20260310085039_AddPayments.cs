using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EnrollmentPayments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ActivityId = table.Column<long>(type: "bigint", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    MollieId = table.Column<string>(type: "text", nullable: false),
                    PaymentIntentUrl = table.Column<string>(type: "text", nullable: false),
                    PaidAt = table.Column<string>(type: "text", nullable: true),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnrollmentPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnrollmentPayments_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EnrollmentPayments_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MembershipPayments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    MollieId = table.Column<string>(type: "text", nullable: false),
                    PaymentIntentUrl = table.Column<string>(type: "text", nullable: false),
                    PaidAt = table.Column<string>(type: "text", nullable: true),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MembershipPayments_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentPayments_ActivityId",
                table: "EnrollmentPayments",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentPayments_MemberId",
                table: "EnrollmentPayments",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPayments_MemberId",
                table: "MembershipPayments",
                column: "MemberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnrollmentPayments");

            migrationBuilder.DropTable(
                name: "MembershipPayments");
        }
    }
}
