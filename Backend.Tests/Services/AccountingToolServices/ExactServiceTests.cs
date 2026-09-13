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

[Collection("NonParallelEnvironment")]
public class ExactServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PostgresDbContext _db;
    private readonly MockHttpMessageHandler _handler;
    private readonly HttpClient _httpClient;
    private readonly ExactService _service;
    private readonly string? _originalAccountingEnabled;
    private readonly string? _originalExactDivision;
    private readonly string? _originalExactAccessToken;

    public ExactServiceTests()
    {
        _originalAccountingEnabled = Environment.GetEnvironmentVariable("ACCOUNTING_ENABLED");
        _originalExactDivision = Environment.GetEnvironmentVariable("EXACT_DIVISION");
        _originalExactAccessToken = Environment.GetEnvironmentVariable("EXACT_ACCESS_TOKEN");

        // Setup Environment Variables
        Environment.SetEnvironmentVariable("ACCOUNTING_ENABLED", "true");
        Environment.SetEnvironmentVariable("EXACT_DIVISION", "12345");
        Environment.SetEnvironmentVariable("EXACT_ACCESS_TOKEN", "mock_token");

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

        Environment.SetEnvironmentVariable("ACCOUNTING_ENABLED", _originalAccountingEnabled);
        Environment.SetEnvironmentVariable("EXACT_DIVISION", _originalExactDivision);
        Environment.SetEnvironmentVariable("EXACT_ACCESS_TOKEN", _originalExactAccessToken);
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
    public async Task SyncPaymentAsync_NewBegunstigerPayment_SyncsAndReturnsIdUsingConfiguredVatCode()
    {
        // Arrange
        _db.Settings.Add(new Setting { Name = "BegunstigerGLAccount", Value = "8010" });
        _db.Settings.Add(new Setting { Name = "BegunstigerVATCode", Value = "H" });
        _db.Settings.Add(new Setting { Name = "BegunstigerCostCenter", Value = "BEG" });
        _db.Settings.Add(new Setting { Name = "BegunstigerCostUnit", Value = "BU1" });
        await _db.SaveChangesAsync();

        var payment = new BegunstigerPayment
        {
            Id = 80,
            PaymentServiceId = "tr_begunstiger",
            PaymentIntentUrl = "https://mollie.com/pay/begunstiger",
            Price = 10.00m
        };

        var expectedGuid = Guid.NewGuid();
        string? postedBody = null;

        _handler.SendAsyncFunc = (req, ct) =>
        {
            if (req.Method == HttpMethod.Get)
            {
                Assert.Contains("YourRef%20eq%20'Begunstiger%20payment-80", req.RequestUri?.Query);
                var emptyResponse = new { d = new { results = Array.Empty<object>() } };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(emptyResponse))
                });
            }
            else if (req.Method == HttpMethod.Post)
            {
                postedBody = req.Content?.ReadAsStringAsync(ct).Result;
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
        Assert.NotNull(postedBody);
        Assert.Contains("\"GLAccount\":\"8010\"", postedBody);
        Assert.Contains("\"VATCode\":\"H\"", postedBody);
        Assert.Contains("\"CostCenter\":\"BEG\"", postedBody);
        Assert.Contains("\"CostUnit\":\"BU1\"", postedBody);
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

    [Fact]
    public async Task SyncPaymentAsync_WhenAccountingDisabledByEnv_ReturnsGuidEmpty()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ACCOUNTING_ENABLED", "false");
        try
        {
            var payment = new MembershipPayment
            {
                Id = 80,
                PaymentServiceId = "tr_disabled",
                PaymentIntentUrl = "https://mollie.com/pay/disabled"
            };

            // Act
            var result = await _service.SyncPaymentAsync(payment, CancellationToken.None);

            // Assert
            Assert.Equal(Guid.Empty, result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ACCOUNTING_ENABLED", "true");
        }
    }

    [Fact]
    public async Task SyncPaymentAsync_WhenAccountingServiceSettingMissing_ReturnsGuidEmpty()
    {
        // Arrange
        var setting = await _db.Settings.FirstAsync(s => s.Name == "AccountingService");
        _db.Settings.Remove(setting);
        await _db.SaveChangesAsync();

        var payment = new MembershipPayment
        {
            Id = 81,
            PaymentServiceId = "tr_no_service",
            PaymentIntentUrl = "https://mollie.com/pay/no_service"
        };

        // Act
        var result = await _service.SyncPaymentAsync(payment, CancellationToken.None);

        // Assert
        Assert.Equal(Guid.Empty, result);
    }

    [Fact]
    public async Task SyncPaymentAsync_WithPaymentConditionAndCustomer_IncludesInPostedSalesEntry()
    {
        // Arrange
        _db.Settings.Add(new Setting { Name = "PaymentServicePaymentsCondition", Value = "COND_2" });
        _db.Settings.Add(new Setting { Name = "PaymentServiceRelationCode", Value = "CUST_473" });
        await _db.SaveChangesAsync();

        var payment = new MembershipPayment
        {
            Id = 82,
            PaymentServiceId = "tr_cond",
            PaymentIntentUrl = "https://mollie.com/pay/cond",
            Price = 20.00m
        };

        var expectedGuid = Guid.NewGuid();
        string? postedBody = null;

        _handler.SendAsyncFunc = async (req, ct) =>
        {
            if (req.Method == HttpMethod.Get)
            {
                var emptyResponse = new { d = new { results = Array.Empty<object>() } };
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(emptyResponse))
                };
            }
            else if (req.Method == HttpMethod.Post)
            {
                postedBody = await req.Content!.ReadAsStringAsync(ct);
                var createdResponse = new { ID = expectedGuid };
                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(JsonSerializer.Serialize(createdResponse))
                };
            }

            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        };

        // Act
        var result = await _service.SyncPaymentAsync(payment, CancellationToken.None);

        // Assert
        Assert.Equal(expectedGuid, result);
        Assert.NotNull(postedBody);
        Assert.Contains("\"PaymentCondition\":\"COND_2\"", postedBody);
        Assert.Contains("\"Customer\":\"CUST_473\"", postedBody);
    }

    [Fact]
    public async Task SyncPaymentAsync_EnrollmentPaymentWithoutGLAccount_UsesActivityGLAccountFallback()
    {
        // Arrange - no GLAccountId on activity, Organizer is null
        _db.Settings.Add(new Setting { Name = "ActivityGLAccount", Value = "7050" });
        await _db.SaveChangesAsync();

        var activity = new Activity
        {
            Id = 99,
            Name = "Fallback Activity",
            Price = 12.0m,
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow,
            DateTimeEnd = DateTime.UtcNow.AddHours(2),
            Location = "Taverndo",
            AllowedAudience = Backend.Models.TargetAudience.All,
            PaymentDeadline = DateTimeOffset.UtcNow
        };

        var payment = new EnrollmentPayment
        {
            Id = 83,
            PaymentServiceId = "tr_fallback_act",
            PaymentIntentUrl = "https://mollie.com/pay/fallback_act",
            Price = 12.00m,
            ActivityId = 99,
            Activity = activity
        };

        var expectedGuid = Guid.NewGuid();
        string? postedBody = null;

        _handler.SendAsyncFunc = async (req, ct) =>
        {
            if (req.Method == HttpMethod.Get)
            {
                var emptyResponse = new { d = new { results = Array.Empty<object>() } };
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(emptyResponse))
                };
            }
            else if (req.Method == HttpMethod.Post)
            {
                postedBody = await req.Content!.ReadAsStringAsync(ct);
                var createdResponse = new { ID = expectedGuid };
                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(JsonSerializer.Serialize(createdResponse))
                };
            }

            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        };

        // Act
        var result = await _service.SyncPaymentAsync(payment, CancellationToken.None);

        // Assert
        Assert.Equal(expectedGuid, result);
        Assert.NotNull(postedBody);
        Assert.Contains("\"GLAccount\":\"7050\"", postedBody);
    }
}
