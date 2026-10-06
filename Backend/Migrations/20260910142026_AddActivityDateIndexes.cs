using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityDateIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Activities_DateTimeEnd",
                table: "Activities",
                column: "DateTimeEnd");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_DateTimeStart",
                table: "Activities",
                column: "DateTimeStart");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Activities_DateTimeEnd",
                table: "Activities");

            migrationBuilder.DropIndex(
                name: "IX_Activities_DateTimeStart",
                table: "Activities");
        }
    }
}
