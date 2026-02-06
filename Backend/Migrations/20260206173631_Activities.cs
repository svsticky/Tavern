using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class Activities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Activities",
                newName: "EnglishDescription");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Enrollments",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "AreParticipantsVisible",
                table: "Activities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CostCenterId",
                table: "Activities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CostUnitId",
                table: "Activities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DutchDescription",
                table: "Activities",
                type: "character varying(240)",
                maxLength: 240,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GLAccountId",
                table: "Activities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAdultOnly",
                table: "Activities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsEnrollable",
                table: "Activities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOpenForPayment",
                table: "Activities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOpenToFirstYears",
                table: "Activities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOpenToMasters",
                table: "Activities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOpenToSecondYears",
                table: "Activities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOpenToThirdYearsAndAbove",
                table: "Activities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Activities",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "OrganizerId",
                table: "Activities",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "ParticipantLimit",
                table: "Activities",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PosterFileName",
                table: "Activities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PosterPath",
                table: "Activities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Activities",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "ShowInKoala",
                table: "Activities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowOnWebsite",
                table: "Activities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UnenrollmentDeadline",
                table: "Activities",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<long>(
                name: "VatRate",
                table: "Activities",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "SpecificationQuestions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ActivityId = table.Column<long>(type: "bigint", nullable: false),
                    QuestionDutch = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    QuestionEnglish = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsMandatory = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecificationQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecificationQuestions_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpecificationAnswers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SpecificationQuestionId = table.Column<long>(type: "bigint", nullable: false),
                    EnrollmentId = table.Column<long>(type: "bigint", nullable: false),
                    Answer = table.Column<string>(type: "text", nullable: false),
                    EnrollmentActivityId = table.Column<long>(type: "bigint", nullable: false),
                    EnrollmentMemberId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecificationAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecificationAnswers_Enrollments_EnrollmentActivityId_Enrol~",
                        columns: x => new { x.EnrollmentActivityId, x.EnrollmentMemberId },
                        principalTable: "Enrollments",
                        principalColumns: new[] { "ActivityId", "MemberId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpecificationAnswers_SpecificationQuestions_SpecificationQu~",
                        column: x => x.SpecificationQuestionId,
                        principalTable: "SpecificationQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_OrganizerId",
                table: "Activities",
                column: "OrganizerId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationAnswers_EnrollmentActivityId_EnrollmentMemberId",
                table: "SpecificationAnswers",
                columns: new[] { "EnrollmentActivityId", "EnrollmentMemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationAnswers_SpecificationQuestionId",
                table: "SpecificationAnswers",
                column: "SpecificationQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationQuestions_ActivityId",
                table: "SpecificationQuestions",
                column: "ActivityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Groups_OrganizerId",
                table: "Activities",
                column: "OrganizerId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Groups_OrganizerId",
                table: "Activities");

            migrationBuilder.DropTable(
                name: "SpecificationAnswers");

            migrationBuilder.DropTable(
                name: "SpecificationQuestions");

            migrationBuilder.DropIndex(
                name: "IX_Activities_OrganizerId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "AreParticipantsVisible",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CostCenterId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CostUnitId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "DutchDescription",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "GLAccountId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "IsAdultOnly",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "IsEnrollable",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "IsOpenForPayment",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "IsOpenToFirstYears",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "IsOpenToMasters",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "IsOpenToSecondYears",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "IsOpenToThirdYearsAndAbove",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "OrganizerId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "ParticipantLimit",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "PosterFileName",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "PosterPath",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "ShowInKoala",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "ShowOnWebsite",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "UnenrollmentDeadline",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "Activities");

            migrationBuilder.RenameColumn(
                name: "EnglishDescription",
                table: "Activities",
                newName: "Description");
        }
    }
}
