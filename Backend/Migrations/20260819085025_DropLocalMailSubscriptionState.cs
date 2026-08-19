using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class DropLocalMailSubscriptionState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Mailinglists");

            migrationBuilder.DropColumn(
                name: "MailSubscriptions",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "MailSubscription",
                table: "MailSubscriptionOutboxTasks");

            migrationBuilder.AddColumn<string>(
                name: "OldEmail",
                table: "MailSubscriptionOutboxTasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubscribedListIdsJson",
                table: "MailSubscriptionOutboxTasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaskType",
                table: "MailSubscriptionOutboxTasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OldEmail",
                table: "MailSubscriptionOutboxTasks");

            migrationBuilder.DropColumn(
                name: "SubscribedListIdsJson",
                table: "MailSubscriptionOutboxTasks");

            migrationBuilder.DropColumn(
                name: "TaskType",
                table: "MailSubscriptionOutboxTasks");

            migrationBuilder.AddColumn<long>(
                name: "MailSubscriptions",
                table: "Members",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "MailSubscription",
                table: "MailSubscriptionOutboxTasks",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "Mailinglists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BitValue = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ServiceId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mailinglists", x => x.Id);
                });
        }
    }
}
