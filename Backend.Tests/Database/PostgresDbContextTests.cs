using Backend.Database;
using Backend.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Database;

public class PostgresDbContextTests
{
    [Fact]
    public void Can_Initialize_PostgresDbContext_And_Perform_Basic_Operations()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        // Act & Assert
        using (var db = new PostgresDbContext(options))
        {
            db.Database.EnsureCreated();

            var setting = new Setting { Name = "TestSetting", Value = "TestValue" };
            db.Settings.Add(setting);
            db.SaveChanges();

            var retrieved = db.Settings.Find("TestSetting");
            Assert.NotNull(retrieved);
            Assert.Equal("TestValue", retrieved.Value);
        }
    }
}
