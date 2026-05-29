using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Database;
using Backend.Models.Domain;
using Backend.Services.PaymentServices;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mollie.Api.Client.Abstract;
using Mollie.Api.Models;
using Mollie.Api.Models.Payment.Request;
using Mollie.Api.Models.Payment.Response;
using Mollie.Api.Models.Url;
using NSubstitute;
using Xunit;

namespace Backend.Tests.Services.PaymentServices;

public class PaymentServicesTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PostgresDbContext _db;
    private readonly IPaymentClient _mollieClientMock;
    private readonly MollieService _service;

    public PaymentServicesTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new PostgresDbContext(dbOptions);
        _db.Database.EnsureCreated();

        _mollieClientMock = Substitute.For<IPaymentClient>();
        _service = new MollieService(_mollieClientMock, _db, NullLogger<MollieService>.Instance);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
        Environment.SetEnvironmentVariable("ACCOUNTING_SERVICE", null);
    }

    private PaymentResponse CreateMockPaymentResponse(string id, string status, DateTimeOffset? paidAt = null)
    {
        return new PaymentResponse
        {
            Id = id,
            Status = status,
            PaidAt = paidAt?.UtcDateTime,
            Resource = "payment",
            Mode = Mode.Test,
            CreatedAt = DateTime.UtcNow,
            Amount = new Amount(Currency.EUR, 7.50m),
            ProfileId = "pfl_123",
            SequenceType = "oneoff",
            Links = new PaymentResponseLinks
            {
                Self = new UrlObjectLink<PaymentResponse> { Href = "http://self", Type = "application/hal+json" },
                Dashboard = new UrlObjectLink<PaymentResponse> { Href = "http://dashboard", Type = "application/hal+json" },
                Documentation = new UrlObjectLink<PaymentResponse> { Href = "http://docs", Type = "application/hal+json" }
            }
        };
    }

    [Theory]
    [InlineData("paid", PaymentStatus.Paid)]
    [InlineData("open", PaymentStatus.Pending)]
    [InlineData("pending", PaymentStatus.Pending)]
    [InlineData("cancelled", PaymentStatus.Failed)]
    [InlineData("failed", PaymentStatus.Failed)]
    [InlineData("expired", PaymentStatus.Failed)]
    public async Task GetPaymentAsync_MapsStatusCorrectly(string mollieStatus, PaymentStatus expectedStatus)
    {
        // Arrange
        var mockResponse = CreateMockPaymentResponse("tr_123", mollieStatus, DateTime.UtcNow);
        _mollieClientMock.GetPaymentAsync("tr_123").Returns(mockResponse);

        // Act
        var result = await _service.GetPaymentAsync("tr_123");

        // Assert
        Assert.Equal("tr_123", result.PaymentId);
        Assert.Equal(expectedStatus, result.Status);
    }

    [Fact]
    public async Task GetPaymentAsync_UnknownStatus_ThrowsException()
    {
        // Arrange
        var mockResponse = CreateMockPaymentResponse("tr_123", "unknown_status");
        _mollieClientMock.GetPaymentAsync("tr_123").Returns(mockResponse);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.GetPaymentAsync("tr_123"));
    }

    [Fact]
    public async Task CancelPaymentAsync_CallsMollieClient()
    {
        // Act
        await _service.CancelPaymentAsync("tr_123");

        // Assert
        await _mollieClientMock.Received(1).CancelPaymentAsync("tr_123");
    }

    [Fact]
    public async Task CreatePaymentAsync_Success_ReturnsCheckoutResponse()
    {
        // Arrange
        var response = CreateMockPaymentResponse("tr_abc", "open");
        response.Links.Checkout = new UrlLink { Href = "https://checkout.mollie.com/pay/abc", Type = "text/html" };
        _mollieClientMock.CreatePaymentAsync(Arg.Any<PaymentRequest>()).Returns(response);

        // Act
        var result = await _service.CreatePaymentAsync(7.50m, "Description", "http://redirect", "http://webhook");

        // Assert
        Assert.Equal("tr_abc", result.PaymentId);
        Assert.Equal("https://checkout.mollie.com/pay/abc", result.PaymentUrl);
    }

    [Fact]
    public async Task CreatePaymentAsync_NullCheckoutLink_ThrowsInvalidOperationException()
    {
        // Arrange
        var response = CreateMockPaymentResponse("tr_abc", "open");
        response.Links.Checkout = null; // Set checkout link to null explicitly
        _mollieClientMock.CreatePaymentAsync(Arg.Any<PaymentRequest>()).Returns(response);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreatePaymentAsync(7.50m, "Description"));
    }

    [Fact]
    public async Task HandleWebhookAsync_PaymentNotFound_ThrowsException()
    {
        // Arrange
        var response = CreateMockPaymentResponse("tr_missing", "paid");
        _mollieClientMock.GetPaymentAsync("tr_missing").Returns(response);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _service.HandleWebhookAsync("tr_missing"));
        Assert.Equal("Payment not found", ex.Message);
    }

    [Fact]
    public async Task HandleWebhookAsync_PaidStatus_ProcessesPaymentsSuccessfully()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ACCOUNTING_SERVICE", "exact");

        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Dave",
            LastName = "Miller",
            Email = "dave@example.com",
            StudentNumber = "s9",
            PhoneNumber = "9",
            Street = "St",
            HouseNumber = "9",
            PostalCode = "9",
            City = "Enschede",
            AuthSystemUserId = Guid.NewGuid()
        };

        var membershipPayment = new MembershipPayment
        {
            Id = 1,
            PaymentServiceId = "tr_paid_1",
            PaymentIntentUrl = "url",
            Price = 10,
            Member = member
        };

        var enrollmentPayment = new EnrollmentPayment
        {
            Id = 2,
            PaymentServiceId = "tr_paid_1",
            PaymentIntentUrl = "url",
            Price = 5,
            Member = member
        };

        _db.Members.Add(member);
        _db.MembershipPayments.Add(membershipPayment);
        _db.EnrollmentPayments.Add(enrollmentPayment);
        await _db.SaveChangesAsync();

        var paidTime = DateTimeOffset.UtcNow;
        var mockPaymentResponse = CreateMockPaymentResponse("tr_paid_1", "paid", paidTime);
        _mollieClientMock.GetPaymentAsync("tr_paid_1").Returns(mockPaymentResponse);

        // Create a new instance of MollieService so that it picks up the Environment variable at creation time
        var serviceWithAccounting = new MollieService(_mollieClientMock, _db, NullLogger<MollieService>.Instance);

        // Act
        await serviceWithAccounting.HandleWebhookAsync("tr_paid_1");

        // Assert
        var updatedMembership = await _db.MembershipPayments.FirstAsync(p => p.Id == 1);
        var updatedEnrollment = await _db.EnrollmentPayments.FirstAsync(p => p.Id == 2);

        Assert.NotNull(updatedMembership.PaidAt);
        Assert.NotNull(updatedEnrollment.PaidAt);

        // Verify Auth System Sync Outbox task was added for MembershipPayment
        var authTask = await _db.AuthOutboxTasks.SingleAsync();
        Assert.Equal(AuthTaskType.Sync, authTask.TaskType);
        Assert.Equal(member.AuthSystemUserId, authTask.AuthSystemUserId);

        // Verify Accounting Tool Outbox tasks were added
        var accountingTasks = await _db.AccountingToolOutboxTasks.ToListAsync();
        Assert.Equal(2, accountingTasks.Count);
        Assert.Contains(accountingTasks, t => t.PaymentId == 1 && t.TaskType == AccountingToolTaskType.MembershipPayment);
        Assert.Contains(accountingTasks, t => t.PaymentId == 2 && t.TaskType == AccountingToolTaskType.EnrollmentPayment);
    }
}
