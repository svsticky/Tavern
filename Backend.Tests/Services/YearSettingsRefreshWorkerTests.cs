using System;
using System.Reflection;
using System.Threading.Tasks;
using Backend.Database;
using Backend.Models.Domain;
using Backend.Services;
using Backend.Utils.DateTime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Backend.Tests.Services;

public class YearSettingsRefreshWorkerTests
{
    private readonly DbContextOptions<PostgresDbContext> _dbOptions;
    private readonly ILogger<YearSettingsRefreshWorker> _logger;

    public YearSettingsRefreshWorkerTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _logger = NullLogger<YearSettingsRefreshWorker>.Instance;
    }

    private static ServiceProvider CreateServiceProvider(PostgresDbContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        return services.BuildServiceProvider();
    }

    private class TestableYearSettingsRefreshWorker(IServiceProvider serviceProvider, ILogger<YearSettingsRefreshWorker> logger)
        : YearSettingsRefreshWorker(serviceProvider, logger)
    {
        public Task PublicRefreshAsync()
        {
            var method = typeof(YearSettingsRefreshWorker)
                .GetMethod("RefreshAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null)
            {
                throw new InvalidOperationException("RefreshAsync method not found");
            }
            return (Task)method.Invoke(this, null)!;
        }
    }

    [Fact]
    public async Task RefreshAsync_SettingsChanged_UpdatesYearUtils()
    {
        var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();
        db.Settings.Add(new Setting { Name = "FinancialYearStartDate", Value = "09-15" });
        db.Settings.Add(new Setting { Name = "CommitteeCreationDate", Value = "10-15" });
        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var worker = new TestableYearSettingsRefreshWorker(provider, _logger);

        var originalFinancial = YearUtils.FinancialYearStartDate;
        var originalCommittee = YearUtils.CommitteeCreationDate;
        try
        {
            await worker.PublicRefreshAsync();

            Assert.Equal("09-15", YearUtils.FinancialYearStartDate);
            Assert.Equal("10-15", YearUtils.CommitteeCreationDate);
        }
        finally
        {
            YearUtils.FinancialYearStartDate = originalFinancial;
            YearUtils.CommitteeCreationDate = originalCommittee;
        }
    }

    [Fact]
    public async Task RefreshAsync_NoSettingsConfigured_KeepsExistingValue()
    {
        var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var provider = CreateServiceProvider(db);
        var worker = new TestableYearSettingsRefreshWorker(provider, _logger);

        var originalFinancial = YearUtils.FinancialYearStartDate;
        var originalCommittee = YearUtils.CommitteeCreationDate;
        try
        {
            await worker.PublicRefreshAsync();

            Assert.Equal(originalFinancial, YearUtils.FinancialYearStartDate);
            Assert.Equal(originalCommittee, YearUtils.CommitteeCreationDate);
        }
        finally
        {
            YearUtils.FinancialYearStartDate = originalFinancial;
            YearUtils.CommitteeCreationDate = originalCommittee;
        }
    }

    [Fact]
    public async Task RefreshAsync_DatabaseThrows_LogsAndDoesNotPropagate()
    {
        var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();
        db.Dispose(); // Any query against a disposed context throws ObjectDisposedException.

        var provider = CreateServiceProvider(db);
        var worker = new TestableYearSettingsRefreshWorker(provider, _logger);

        // Should not throw - the failure is caught and logged so a transient DB blip doesn't take
        // the whole background loop down.
        await worker.PublicRefreshAsync();
    }

    [Fact]
    public async Task ExecuteAsync_RunsRefreshLoopThenExitsOnCancellation()
    {
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();
        var provider = CreateServiceProvider(db);
        var worker = new YearSettingsRefreshWorker(provider, _logger);

        var cts = new CancellationTokenSource();
        var startTask = worker.StartAsync(cts.Token);

        // Give the loop a moment to run RefreshAsync once and enter the 30s delay, then cancel so
        // Task.Delay throws TaskCanceledException and the loop exits instead of actually waiting.
        await Task.Delay(50);
        cts.Cancel();

        await worker.StopAsync(CancellationToken.None);
        await startTask;
    }
}
