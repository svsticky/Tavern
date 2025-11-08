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
    /// <summary>Reference to the Enrollments relational table. </summary>
    public DbSet<Enrollment> Enrollments { get; set; }
    /// <summary>Reference to the Members relational table. </summary>
    public DbSet<Member> Members { get; set; }
    /// <summary>Reference to the Studies relational table. </summary>
    public DbSet<Study> Studies { get; set; }
    /// <summary>Reference to the StudyEnrollments relational table. </summary>
    public DbSet<StudyEnrollment> StudyEnrollments { get; set; }
    /// <summary>Reference to the Commissions relational table. </summary>
    public DbSet<Commission> Commissions { get; set; }
    /// <summary>Reference to the CommissionMemberships relational table. </summary>
    public DbSet<CommissionMembership> CommissionMemberships { get; set; }
    /// <summary>Reference to the Roles relational table. </summary>
    public DbSet<Role> Roles { get; set; }

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

        // Member → Enrollment (cascade delete)
        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Member)
            .WithMany(m => m.Enrollments)
            .HasForeignKey(e => e.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        // Activity → Enrollment (cascade delete)
        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Activity)
            .WithMany(a => a.Enrollments)
            .HasForeignKey(e => e.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        // Member → StudyEnrollment (cascade delete)
        modelBuilder.Entity<StudyEnrollment>()
            .HasOne(se => se.Member)
            .WithMany(m => m.StudyEnrollments)
            .HasForeignKey(se => se.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        // Study → StudyEnrollment (cascade delete)
        modelBuilder.Entity<StudyEnrollment>()
            .HasOne(se => se.Study)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(se => se.StudyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Commission → CommissionMembership (cascade delete)
        modelBuilder.Entity<CommissionMembership>()
            .HasOne(cm => cm.Commission)
            .WithMany(c => c.CommissionMemberships)
            .HasForeignKey(cm => cm.CommissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Member → CommissionMembership (cascade delete)
        modelBuilder.Entity<CommissionMembership>()
            .HasOne(cm => cm.Member)
            .WithMany(m => m.CommissionMemberships)
            .HasForeignKey(cm => cm.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        // Role → CommissionMembership (set null on delete)
        modelBuilder.Entity<CommissionMembership>()
            .HasOne(cm => cm.Role)
            .WithMany()
            .HasForeignKey(cm => cm.RoleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
