using Backend.Database;
using Backend.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Services.Domain;

public class TestPostgresDbContext : PostgresDbContext
{
    public TestPostgresDbContext(DbContextOptions<PostgresDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<MembershipPayment>()
            .HasIndex(p => p.MemberId)
            .IsUnique()
            .HasFilter("MemberId IS NOT NULL");
    }
}
