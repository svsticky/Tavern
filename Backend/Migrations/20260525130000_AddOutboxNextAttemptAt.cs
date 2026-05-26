using System;
using Backend.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(PostgresDbContext))]
    [Migration("20260525130000_AddOutboxNextAttemptAt")]
    public partial class AddOutboxNextAttemptAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextAttemptAt",
                table: "AuthOutboxTasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextAttemptAt",
                table: "AccountingToolOutboxTasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextAttemptAt",
                table: "MailSubscriptionOutboxTasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "AuthOutboxTasks"
                SET "NextAttemptAt" = "CreatedAt"
                WHERE "NextAttemptAt" IS NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE "AccountingToolOutboxTasks"
                SET "NextAttemptAt" = "CreatedAt"
                WHERE "NextAttemptAt" IS NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE "MailSubscriptionOutboxTasks"
                SET "NextAttemptAt" = "CreatedAt"
                WHERE "NextAttemptAt" IS NULL;
                """);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "NextAttemptAt",
                table: "AuthOutboxTasks",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "NextAttemptAt",
                table: "AccountingToolOutboxTasks",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "NextAttemptAt",
                table: "MailSubscriptionOutboxTasks",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                table: "AuthOutboxTasks");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                table: "AccountingToolOutboxTasks");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                table: "MailSubscriptionOutboxTasks");
        }
    }
}
