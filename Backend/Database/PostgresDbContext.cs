using Backend.Models;
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
    /// <summary>Reference to the Membership Payments relational table. </summary>
    public DbSet<MembershipPayment> MembershipPayments { get; set; }
    /// <summary>Reference to the Activity Payments relational table. </summary>
    public DbSet<EnrollmentPayment> EnrollmentPayments { get; set; }

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

        // Role → GroupMembership (set null on delete)
        modelBuilder.Entity<GroupMembership>()
            .HasOne(cm => cm.Role)
            .WithMany()
            .HasForeignKey(cm => cm.RoleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
