using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "PaymentSequence");

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

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    DefaultGLAccount = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DefaultCostCenter = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DefaultCostUnit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KeycloakOutboxTasks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KeycloakId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeycloakOutboxTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KeycloakId = table.Column<Guid>(type: "uuid", nullable: true),
                    StudentNumber = table.Column<long>(type: "bigint", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    LastName = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    ParentPhoneNumber = table.Column<string>(type: "text", nullable: true),
                    Street = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    HouseNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    City = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DateOfBirth = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MailSubscriptions = table.Column<long>(type: "bigint", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    RegisteredOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PreferredLanguage = table.Column<int>(type: "integer", nullable: false),
                    Gratie = table.Column<bool>(type: "boolean", nullable: false),
                    LidVanVerdienste = table.Column<bool>(type: "boolean", nullable: false),
                    EreLid = table.Column<bool>(type: "boolean", nullable: false),
                    Begunstiger = table.Column<bool>(type: "boolean", nullable: false),
                    Suspended = table.Column<bool>(type: "boolean", nullable: false),
                    ProfilePicturePath = table.Column<string>(type: "text", nullable: true),
                    ProfilePictureFileName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Studies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NominalDurationYears = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Studies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PosterFileName = table.Column<string>(type: "text", nullable: true),
                    PosterPath = table.Column<string>(type: "text", nullable: true),
                    DutchDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    EnglishDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DateTimeStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DateTimeEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UnenrollmentDeadline = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EnrollmentDeadline = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ParticipantLimit = table.Column<long>(type: "bigint", nullable: true),
                    OrganizerId = table.Column<long>(type: "bigint", nullable: true),
                    ShowInKoala = table.Column<bool>(type: "boolean", nullable: false),
                    ShowOnWebsite = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnrollable = table.Column<bool>(type: "boolean", nullable: false),
                    AreParticipantsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    IsAdultOnly = table.Column<bool>(type: "boolean", nullable: false),
                    AllowedAudience = table.Column<long>(type: "bigint", nullable: false),
                    IsOpenForPayment = table.Column<bool>(type: "boolean", nullable: false),
                    VatRate = table.Column<long>(type: "bigint", nullable: true),
                    GLAccountId = table.Column<string>(type: "text", nullable: true),
                    CostCenterId = table.Column<string>(type: "text", nullable: true),
                    CostUnitId = table.Column<string>(type: "text", nullable: true),
                    PaymentDeadline = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activities_Groups_OrganizerId",
                        column: x => x.OrganizerId,
                        principalTable: "Groups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Announcements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Announcements_Members_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MembershipPayments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "nextval('\"PaymentSequence\"')"),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    MollieId = table.Column<string>(type: "text", nullable: false),
                    PaymentIntentUrl = table.Column<string>(type: "text", nullable: false),
                    PaidAt = table.Column<string>(type: "text", nullable: true),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExactEntryId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MembershipPayments_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
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

            migrationBuilder.CreateTable(
                name: "StudyEnrollments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudyId = table.Column<long>(type: "bigint", nullable: false),
                    EnrollmentDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletionDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyEnrollments_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudyEnrollments_Studies_StudyId",
                        column: x => x.StudyId,
                        principalTable: "Studies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EnrollmentPayments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "nextval('\"PaymentSequence\"')"),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    MollieId = table.Column<string>(type: "text", nullable: false),
                    PaymentIntentUrl = table.Column<string>(type: "text", nullable: false),
                    PaidAt = table.Column<string>(type: "text", nullable: true),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExactEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActivityId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnrollmentPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnrollmentPayments_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EnrollmentPayments_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Enrollments",
                columns: table => new
                {
                    ActivityId = table.Column<long>(type: "bigint", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RegisteredOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsOnWaitingList = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => new { x.ActivityId, x.MemberId });
                    table.ForeignKey(
                        name: "FK_Enrollments_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Enrollments_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    Options = table.Column<string>(type: "text", nullable: true)
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
                name: "GroupMemberships",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<long>(type: "bigint", nullable: false),
                    MembershipYear = table.Column<long>(type: "bigint", nullable: false),
                    RoleAliasId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupMemberships_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupMemberships_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupMemberships_RoleAliases_RoleAliasId",
                        column: x => x.RoleAliasId,
                        principalTable: "RoleAliases",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SpecificationAnswers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SpecificationQuestionId = table.Column<long>(type: "bigint", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Answer = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
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
                        name: "FK_SpecificationAnswers_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
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
                name: "IX_Announcements_CreatedById",
                table: "Announcements",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentPayments_ActivityId",
                table: "EnrollmentPayments",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentPayments_MemberId",
                table: "EnrollmentPayments",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_MemberId",
                table: "Enrollments",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ExactOutboxTasks_PaymentId",
                table: "ExactOutboxTasks",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMemberships_GroupId",
                table: "GroupMemberships",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMemberships_MemberId",
                table: "GroupMemberships",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMemberships_RoleAliasId",
                table: "GroupMemberships",
                column: "RoleAliasId");

            migrationBuilder.CreateIndex(
                name: "IX_Members_Email",
                table: "Members",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_StudentNumber",
                table: "Members",
                column: "StudentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPayments_MemberId",
                table: "MembershipPayments",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAliases_RoleId",
                table: "RoleAliases",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationAnswers_EnrollmentActivityId_EnrollmentMemberId",
                table: "SpecificationAnswers",
                columns: new[] { "EnrollmentActivityId", "EnrollmentMemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationAnswers_MemberId",
                table: "SpecificationAnswers",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationAnswers_SpecificationQuestionId",
                table: "SpecificationAnswers",
                column: "SpecificationQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationQuestions_ActivityId",
                table: "SpecificationQuestions",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyEnrollments_MemberId",
                table: "StudyEnrollments",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyEnrollments_StudyId",
                table: "StudyEnrollments",
                column: "StudyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Announcements");

            migrationBuilder.DropTable(
                name: "EnrollmentPayments");

            migrationBuilder.DropTable(
                name: "ExactOutboxTasks");

            migrationBuilder.DropTable(
                name: "GroupMemberships");

            migrationBuilder.DropTable(
                name: "KeycloakOutboxTasks");

            migrationBuilder.DropTable(
                name: "MembershipPayments");

            migrationBuilder.DropTable(
                name: "SpecificationAnswers");

            migrationBuilder.DropTable(
                name: "StudyEnrollments");

            migrationBuilder.DropTable(
                name: "RoleAliases");

            migrationBuilder.DropTable(
                name: "Enrollments");

            migrationBuilder.DropTable(
                name: "SpecificationQuestions");

            migrationBuilder.DropTable(
                name: "Studies");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Members");

            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropSequence(
                name: "PaymentSequence");
        }
    }
}
