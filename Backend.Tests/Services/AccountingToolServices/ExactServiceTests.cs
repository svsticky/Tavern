using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Backend.Database;
using Backend.Models;
using Backend.Models.Domain;
using Backend.Services.AccountingToolServices;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Backend.Tests.Services.AccountingToolServices;

public class MockHttpMessageHandler : HttpMessageHandler
{
    public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> SendAsyncFunc { get; set; } = null!;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return SendAsyncFunc(request, cancellationToken);
    }
}

public class ExactServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PostgresDbContext _db;
    private readonly MockHttpMessageHandler _handler;
    private readonly HttpClient _httpClient;
    private readonly ExactService _service;

    public ExactServiceTests()
    {
        // Setup Environment Variables
        Environment.SetEnvironmentVariable("EXACT_DIVISION", "12345");
        Environment.SetEnvironmentVariable("EXACT_ACCESS_TOKEN", "mock_token");
        Environment.SetEnvironmentVariable("PAYMENT_PROVIDER", "mollie");

        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new PostgresDbContext(dbOptions);
        _db.Database.EnsureCreated();

        _db.Settings.Add(new Setting { Name = "AccountingService", Value = "EXACT" });
        _db.Settings.Add(new Setting { Name = "ExactDivision", Value = "12345" });
        _db.Settings.Add(new Setting { Name = "ExactAccessToken", Value = "mock_token" });
        _db.SaveChanges();

        _handler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("https://api.exactonline.nl/")
        };

        _service = new ExactService(_httpClient, _db, NullLogger<ExactService>.Instance);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
        _httpClient.Dispose();
    }

    [Fact]
    public async Task SyncPaymentAsync_PaymentNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.SyncPaymentAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task SyncPaymentAsync_ExistingSalesEntry_ReturnsExistingId()
    {
        // Arrange
        var payment = new MembershipPayment
        {
            Id = 42,
            PaymentServiceId = "tr_123",
            PaymentIntentUrl = "https://mollie.com/pay/123"
        };

        var expectedGuid = Guid.NewGuid();

        // Mock GET SalesEntries returning existing entry
        _handler.SendAsyncFunc = (req, ct) =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Contains("YourRef%20eq%20'Membership%20payment-42", req.RequestUri?.Query);

            var responseJson = new
            {
                d = new
                {
                    results = new[]
                    {
                        new { ID = expectedGuid }
                    }
                }
            };

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(responseJson))
            });
        };

        // Act
        var result = await _service.SyncPaymentAsync(payment, CancellationToken.None);

        // Assert
        Assert.Equal(expectedGuid, result);
    }

    [Fact]
    public async Task SyncPaymentAsync_NewMembershipPayment_SyncsAndReturnsId()
    {
        // Arrange
        _db.Settings.Add(new Setting { Name = "MembershipGLAccount", Value = "9999" });
        await _db.SaveChangesAsync();

        var payment = new MembershipPayment
        {
            Id = 45,
            PaymentServiceId = "tr_456",
            PaymentIntentUrl = "https://mollie.com/pay/456",
            Price = 15.50m
        };

        var expectedGuid = Guid.NewGuid();
        var requestsMade = 0;

        _handler.SendAsyncFunc = (req, ct) =>
        {
            requestsMade++;
            if (req.Method == HttpMethod.Get)
            {
                // Return empty results (does not exist yet)
                var emptyResponse = new { d = new { results = Array.Empty<object>() } };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(emptyResponse))
                });
            }
            else if (req.Method == HttpMethod.Post)
            {
                // Verify the SalesEntry structure built
                Assert.Contains("12345/salesentry/SalesEntries", req.RequestUri?.AbsolutePath);
                var createdResponse = new { ID = expectedGuid };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(JsonSerializer.Serialize(createdResponse))
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
        };

        // Act
        var result = await _service.SyncPaymentAsync(payment, CancellationToken.None);

        // Assert
        Assert.Equal(expectedGuid, result);
        Assert.Equal(2, requestsMade);
    }

    [Fact]
    public async Task SyncPaymentAsync_NewEnrollmentPayment_SyncsAndReturnsId()
    {
        // Arrange
        var activity = new Activity
        {
            Id = 1,
            Name = "Borrel",
            GLAccountId = "8001",
            Price = 5.0m,
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow,
            DateTimeEnd = DateTime.UtcNow.AddHours(2),
            Location = "Taverndo",
            AllowedAudience = Backend.Models.TargetAudience.All,
            PaymentDeadline = DateTimeOffset.UtcNow,
            VatRate = 9
        };

        var payment = new EnrollmentPayment
        {
            Id = 50,
            PaymentServiceId = "tr_789",
            PaymentIntentUrl = "https://mollie.com/pay/789",
            Price = 5.00m,
            ActivityId = 1,
            Activity = activity
        };

        var expectedGuid = Guid.NewGuid();

        _handler.SendAsyncFunc = (req, ct) =>
        {
            if (req.Method == HttpMethod.Get)
            {
                var emptyResponse = new { d = new { results = Array.Empty<object>() } };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(emptyResponse))
                });
            }
            else if (req.Method == HttpMethod.Post)
            {
                var createdResponse = new { ID = expectedGuid };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(JsonSerializer.Serialize(createdResponse))
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
        };

        // Act
        var result = await _service.SyncPaymentAsync(payment, CancellationToken.None);

        // Assert
        Assert.Equal(expectedGuid, result);
    }

    [Fact]
    public async Task SyncPaymentAsync_NewPaymentFeePayment_SyncsAndReturnsId()
    {
        // Arrange
        _db.Settings.Add(new Setting { Name = "PaymentServiceFeeGLAccount", Value = "4444" });
        await _db.SaveChangesAsync();

        var payment = new PaymentServiceFeePayment
        {
            Id = 60,
            PaymentServiceId = "tr_fee",
            PaymentIntentUrl = "https://mollie.com/pay/fee",
            Price = 0.29m
        };

        var expectedGuid = Guid.NewGuid();

        _handler.SendAsyncFunc = (req, ct) =>
        {
            if (req.Method == HttpMethod.Get)
            {
                var emptyResponse = new { d = new { results = Array.Empty<object>() } };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(emptyResponse))
                });
            }
            else if (req.Method == HttpMethod.Post)
            {
                var createdResponse = new { ID = expectedGuid };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(JsonSerializer.Serialize(createdResponse))
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
        };

        // Act
        var result = await _service.SyncPaymentAsync(payment, CancellationToken.None);

        // Assert
        Assert.Equal(expectedGuid, result);
    }

    [Fact]
    public async Task SyncPaymentAsync_HttpFailure_ThrowsException()
    {
        // Arrange
        var payment = new MembershipPayment
        {
            Id = 70,
            PaymentServiceId = "tr_fail",
            PaymentIntentUrl = "https://mollie.com/pay/fail"
        };

        _handler.SendAsyncFunc = (req, ct) =>
        {
            if (req.Method == HttpMethod.Get)
            {
                var emptyResponse = new { d = new { results = Array.Empty<object>() } };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(emptyResponse))
                });
            }
            else if (req.Method == HttpMethod.Post)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("Server crashed")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _service.SyncPaymentAsync(payment, CancellationToken.None));
        Assert.Contains("Exact sync failed: Server crashed", exception.Message);
    }
}
