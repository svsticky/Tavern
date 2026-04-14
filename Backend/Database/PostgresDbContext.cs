using Backend.Models.Domain;
using Microsoft.EntityFrameworkCore;

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
    /// <summary>Reference to the KeycloakOutboxTasks relational table. </summary>
    public DbSet<KeycloakOutboxTask> KeycloakOutboxTasks { get; set; }
    /// <summary>Reference to the ExactOutboxTasks relational table. </summary>
    public DbSet<AccountingToolOutboxTask> AccountingToolOutboxTasks { get; set; }
    /// <summary>Reference to the Membership Payments relational table. </summary>
    public DbSet<MembershipPayment> MembershipPayments { get; set; }
    /// <summary>Reference to the Activity Payments relational table. </summary>
    public DbSet<EnrollmentPayment> EnrollmentPayments { get; set; }
    /// <summary>Reference to the Mollie Fee Payments relational table. </summary>
    public DbSet<MollieFeePayment> MollieFeePayments { get; set; }
    /// <summary>Reference to the Settings relational table. </summary>
    public DbSet<Setting> Settings { get; set; }

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
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasOne(e => e.Member)
                .WithMany(m => m.Enrollments)
                .HasForeignKey(e => e.MemberId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
