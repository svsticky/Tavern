using Backend.Models.Domain;
using Microsoft.EntityFrameworkCore;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Backend.Tests")]

namespace Backend.Database;

/// <summary>
/// Contains all information on the postgresql database, for the object-database mapping.
/// This allows using C# to run SQL queries on the database.
/// </summary>
public class PostgresDbContext : DbContext
{
    /// <summary>Reference to the Activities relational table. </summary>
    public DbSet<Activity> Activities { get; set; }
    /// <summary>Reference to the SpecificationQuestions relational table. </summary>
    public DbSet<SpecificationQuestion> SpecificationQuestions { get; set; }
    /// <summary>Reference to the SpecificationAnswers relational table. </summary>
    public DbSet<SpecificationAnswer> SpecificationAnswers { get; set; }
    /// <summary>Reference to the Enrollments relational table. </summary>
    public DbSet<Enrollment> Enrollments { get; set; }
    /// <summary>Reference to the Members relational table. </summary>
    public DbSet<Member> Members { get; set; }
    /// <summary>Reference to the Studies relational table. </summary>
    public DbSet<Study> Studies { get; set; }
    /// <summary>Reference to the StudyEnrollments relational table. </summary>
    public DbSet<StudyEnrollment> StudyEnrollments { get; set; }
    /// <summary>Reference to the Groups relational table. </summary>
    public DbSet<Group> Groups { get; set; }
    /// <summary>Reference to the GroupMemberships relational table. </summary>
    public DbSet<GroupMembership> GroupMemberships { get; set; }
    /// <summary>Reference to the Roles relational table. </summary>
    public DbSet<Role> Roles { get; set; }
    /// <summary>Reference to the Announcements relational table. </summary>
    public DbSet<Announcement> Announcements { get; set; }
    /// <summary>Reference to the RoleAliases relational table. </summary>
    public DbSet<RoleAlias> RoleAliases { get; set; }
    /// <summary>Reference to the AuthOutboxTasks relational table. </summary>
    public DbSet<AuthOutboxTask> AuthOutboxTasks { get; set; }
    /// <summary>Reference to the ExactOutboxTasks relational table. </summary>
    public DbSet<AccountingToolOutboxTask> AccountingToolOutboxTasks { get; set; }
    /// <summary>Reference to the Membership Payments relational table. </summary>
    public DbSet<MembershipPayment> MembershipPayments { get; set; }
    /// <summary>Reference to the Activity Payments relational table. </summary>
    public DbSet<EnrollmentPayment> EnrollmentPayments { get; set; }
    /// <summary>Reference to the Payment Service Fee Payments relational table. </summary>
    public DbSet<PaymentServiceFeePayment> PaymentServiceFeePayments { get; set; }
    /// <summary>Reference to the Begunstiger Payments relational table. </summary>
    public DbSet<BegunstigerPayment> BegunstigerPayments { get; set; }
    /// <summary>Reference to the Settings relational table. </summary>
    public DbSet<Setting> Settings { get; set; }
    /// <summary>Reference to the MailSubscriptionOutboxTasks relational table. </summary>
    public DbSet<MailSubscriptionOutboxTask> MailSubscriptionOutboxTasks { get; set; }
    /// <summary>Reference to the OutlineOutboxTasks relational table. </summary>
    public DbSet<OutlineOutboxTask> OutlineOutboxTasks { get; set; }
    /// <summary>Reference to the RegistrationDocuments relational table. </summary>
    public DbSet<RegistrationDocument> RegistrationDocuments { get; set; }

    /// <summary>Reference to the RegisterReasons relational table. </summary>
    public DbSet<RegisterReason> RegisterReasons { get; set; }
    /// <summary>Reference to the RegisterSlides relational table. </summary>
    public DbSet<RegisterSlide> RegisterSlides { get; set; }
    /// <summary>Reference to the ExternalLinks relational table. </summary>
    public DbSet<ExternalLink> ExternalLinks { get; set; }
    /// <summary>Reference to the CuratedMailinglists relational table. </summary>
    public DbSet<CuratedMailinglist> CuratedMailinglists { get; set; }

    /// <summary>
    /// Creates information how to set up the object-database mapping, from C# to SQL, on the postgresql database.
    /// </summary>
    /// <param name="options">All parameters that define the database connection.</param>
    public PostgresDbContext(DbContextOptions<PostgresDbContext> options) : base(options)
    { }

    /// <summary>
    /// Define advanced properties of the database.
    /// </summary>
    /// <param name="modelBuilder"></param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Payment>().UseTpcMappingStrategy();

        modelBuilder.Entity<MembershipPayment>(entity =>
        {
            entity.ToTable("MembershipPayments");

            entity.HasOne(p => p.Member)
                .WithMany()
                .HasForeignKey(p => p.MemberId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<EnrollmentPayment>(entity =>
        {
            entity.ToTable("EnrollmentPayments");

            entity.HasOne(p => p.Member)
                .WithMany()
                .HasForeignKey(p => p.MemberId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(p => p.Activity)
                .WithMany()
                .HasForeignKey(p => p.ActivityId)
                .OnDelete(DeleteBehavior.SetNull);

            // Backs the correlated "sum of paid amounts per enrollment" subquery used by
            // GetAllUnpaidEnrollments/GetUnpaidEnrollmentsForMember/GetAllOverpaidEnrollments, which
            // filters on ActivityId + MemberId + PaidAt for every enrollment. Without this composite
            // index, Postgres falls back to the single-column ActivityId/MemberId FK indexes (or a
            // scan), so those queries slow down linearly with the whole EnrollmentPayments history.
            entity.HasIndex(p => new { p.ActivityId, p.MemberId })
                .HasFilter("\"PaidAt\" IS NOT NULL");
        });

        modelBuilder.Entity<BegunstigerPayment>(entity =>
        {
            entity.ToTable("BegunstigerPayments");

            entity.HasOne(p => p.Member)
                .WithMany()
                .HasForeignKey(p => p.MemberId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasOne(e => e.Member)
                .WithMany(m => m.Enrollments)
                .HasForeignKey(e => e.MemberId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // NOTE: intentionally not configured via modelBuilder.Entity<MembershipPayment>().HasIndex(...).
        // Because MemberId is declared on the shared abstract Payment base class and Payment uses the
        // TPC mapping strategy, EF Core hoists any fluent index config for MemberId onto the base
        // entity, which then propagates the same unique index to EnrollmentPayments and
        // PaymentServiceFeePayments too - wrongly limiting members to a single activity payment ever.
        // The unique index is created directly on MembershipPayments only via raw SQL in the migration.

        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasQueryFilter(m => !m.IsDeleted);

            entity.HasIndex(m => m.Email)
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");

            entity.HasIndex(m => m.StudentNumber)
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");
        });
    }
}
