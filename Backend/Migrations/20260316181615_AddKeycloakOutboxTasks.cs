using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddKeycloakOutboxTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupMemberships_Roles_RoleId",
                table: "GroupMemberships");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "GroupMemberships",
                newName: "RoleAliasId");

            migrationBuilder.RenameIndex(
                name: "IX_GroupMemberships_RoleId",
                table: "GroupMemberships",
                newName: "IX_GroupMemberships_RoleAliasId");

            migrationBuilder.AddColumn<Guid>(
                name: "KeycloakId",
                table: "Members",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KeyCloakOutboxTasks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KeycoakId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyCloakOutboxTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleAliases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleAliases_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleAliases_RoleId",
                table: "RoleAliases",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupMemberships_RoleAliases_RoleAliasId",
                table: "GroupMemberships",
                column: "RoleAliasId",
                principalTable: "RoleAliases",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupMemberships_RoleAliases_RoleAliasId",
                table: "GroupMemberships");

            migrationBuilder.DropTable(
                name: "KeyCloakOutboxTasks");

            migrationBuilder.DropTable(
                name: "RoleAliases");

            migrationBuilder.DropColumn(
                name: "KeycloakId",
                table: "Members");

            migrationBuilder.RenameColumn(
                name: "RoleAliasId",
                table: "GroupMemberships",
                newName: "RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_GroupMemberships_RoleAliasId",
                table: "GroupMemberships",
                newName: "IX_GroupMemberships_RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupMemberships_Roles_RoleId",
                table: "GroupMemberships",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
