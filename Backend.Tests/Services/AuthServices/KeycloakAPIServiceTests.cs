using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Services;
using Backend.Services.AuthServices;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Backend.Tests.Services.AuthServices;

public class MockAuthHttpMessageHandler : HttpMessageHandler
{
    public Func<HttpRequestMessage, Task<HttpResponseMessage>> SendAsyncFunc { get; set; } = null!;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
    {
        return SendAsyncFunc(request);
    }
}

public class KeycloakAPIServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PostgresDbContext _db;
    private readonly MailSubscriptionOutboxWorker _mailWorker;
    private readonly IPaymentValidationService _paymentMock;
    private readonly IHttpClientFactory _clientFactoryMock;
    private readonly MockAuthHttpMessageHandler _tokenHandler;
    private readonly MockAuthHttpMessageHandler _adminHandler;
    private readonly HttpClient _tokenClient;
    private readonly HttpClient _adminClient;
    private readonly KeycloakAPIService _service;

    public KeycloakAPIServiceTests()
    {
        Environment.SetEnvironmentVariable("KeycloakUrl", "https://keycloak.local");
        Environment.SetEnvironmentVariable("KeycloakRealm", "tavern");
        Environment.SetEnvironmentVariable("KeycloakBackendClientId", "backend-client");
        Environment.SetEnvironmentVariable("KeycloakClientSecret", "client-secret");

        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new PostgresDbContext(dbOptions);
        _db.Database.EnsureCreated();

        _mailWorker = new MailSubscriptionOutboxWorker(null!, NullLogger<MailSubscriptionOutboxWorker>.Instance);
        _paymentMock = Substitute.For<IPaymentValidationService>();

        _tokenHandler = new MockAuthHttpMessageHandler();
        _tokenClient = new HttpClient(_tokenHandler);

        _adminHandler = new MockAuthHttpMessageHandler();
        _adminClient = new HttpClient(_adminHandler)
        {
            BaseAddress = new Uri("https://keycloak.local/admin/realms/tavern/")
        };

        _clientFactoryMock = Substitute.For<IHttpClientFactory>();
        _clientFactoryMock.CreateClient().Returns(_tokenClient);
        _clientFactoryMock.CreateClient("KeycloakAdmin").Returns(_adminClient);

        _service = new KeycloakAPIService(
            _db,
            _mailWorker,
            _clientFactoryMock,
            _paymentMock,
            NullLogger<KeycloakAPIService>.Instance
        );

        // Standard mock response for token acquisition
        _tokenHandler.SendAsyncFunc = (req) =>
        {
            var tokenResponse = new { access_token = "mock_access_token" };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(tokenResponse))
            });
        };
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
        _tokenClient.Dispose();
        _adminClient.Dispose();
    }

    [Fact]
    public async Task SyncMember_MemberNotFound_ReturnsImmediately()
    {
        // Act
        var missingGuid = Guid.NewGuid();
        await _service.SyncMember(missingGuid);

        // Assert - should not try to contact Keycloak
        // SendAsyncFunc of _adminHandler is not defined so calling it would crash; since it returns immediately, it won't crash.
    }

    [Fact]
    public async Task SyncMember_EmailNotChanged_SyncsToKeycloakSuccessfully()
    {
        // Arrange
        var keycloakId = Guid.NewGuid();
        var member = new Member
        {
            Id = Guid.NewGuid(),
            AuthSystemUserId = keycloakId,
            FirstName = "Alice",
            LastName = "Jones",
            Email = "alice@example.com",
            StudentNumber = "s1111111",
            PhoneNumber = "+31600000000",
            Street = "St",
            HouseNumber = "1",
            PostalCode = "1234AB",
            City = "Enschede"
        };
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _paymentMock.HasPaidMembershipPaymentBeforeExpirationTime(member.Id).Returns(true);

        _adminHandler.SendAsyncFunc = (req) =>
        {
            if (req.Method == HttpMethod.Get && req.RequestUri?.AbsolutePath.EndsWith($"users/{keycloakId}") == true)
            {
                var userResponse = new { email = "alice@example.com" };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(userResponse))
                });
            }
            if (req.Method == HttpMethod.Put && req.RequestUri?.AbsolutePath.EndsWith($"users/{keycloakId}") == true)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        };

        // Act
        await _service.SyncMember(keycloakId);

        // Assert
        // Verified by SendAsyncFunc running without exceptions
    }

    [Fact]
    public async Task SyncMember_Begunstiger_UsesBegunstigerFeeCheckForAccessLevelInsteadOfMembershipCheck()
    {
        // Arrange
        var keycloakId = Guid.NewGuid();
        var member = new Member
        {
            Id = Guid.NewGuid(),
            AuthSystemUserId = keycloakId,
            Begunstiger = true,
            FirstName = "Bea",
            LastName = "Vermeer",
            Email = "bea@example.com",
            StudentNumber = "s2222222",
            PhoneNumber = "+31600000001",
            Street = "St",
            HouseNumber = "2",
            PostalCode = "1234AB",
            City = "Enschede"
        };
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        // The member has never had a qualifying study/membership payment, so the regular check would
        // say "not paid" - but they did pay their begunstiger fee, so access_level should still be "full".
        _paymentMock.HasPaidMembershipPaymentBeforeExpirationTime(member.Id).Returns(false);
        _paymentMock.HasPaidBegunstigerFeeSinceLastBoardChange(member.Id).Returns(true);

        string? putBody = null;

        _adminHandler.SendAsyncFunc = (req) =>
        {
            if (req.Method == HttpMethod.Get && req.RequestUri?.AbsolutePath.EndsWith($"users/{keycloakId}") == true)
            {
                var userResponse = new { email = "bea@example.com" };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(userResponse))
                });
            }
            if (req.Method == HttpMethod.Put && req.RequestUri?.AbsolutePath.EndsWith($"users/{keycloakId}") == true)
            {
                putBody = req.Content?.ReadAsStringAsync().Result;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        };

        // Act
        await _service.SyncMember(keycloakId);

        // Assert
        _paymentMock.Received(1).HasPaidBegunstigerFeeSinceLastBoardChange(member.Id);
        _paymentMock.DidNotReceive().HasPaidMembershipPaymentBeforeExpirationTime(Arg.Any<Guid>());

        Assert.NotNull(putBody);
        Assert.Contains("\"access_level\":[\"full\"]", putBody);
    }

    [Fact]
    public async Task SyncMember_EmailChanged_UpdatesLocalDatabaseAndEnqueuesMailSubscriptionTasks()
    {
        // Arrange
        var keycloakId = Guid.NewGuid();
        var member = new Member
        {
            Id = Guid.NewGuid(),
            AuthSystemUserId = keycloakId,
            FirstName = "Bob",
            LastName = "Smith",
            Email = "bob@example.com",
            StudentNumber = "s2222222",
            PhoneNumber = "+31600000000",
            Street = "St",
            HouseNumber = "1",
            PostalCode = "1234AB",
            City = "Enschede"
        };
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _paymentMock.HasPaidMembershipPaymentBeforeExpirationTime(member.Id).Returns(false);

        _adminHandler.SendAsyncFunc = (req) =>
        {
            if (req.Method == HttpMethod.Get && req.RequestUri?.AbsolutePath.EndsWith($"users/{keycloakId}") == true)
            {
                var userResponse = new { email = "newbob@example.com" };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(userResponse))
                });
            }
            if (req.Method == HttpMethod.Put && req.RequestUri?.AbsolutePath.EndsWith($"users/{keycloakId}") == true)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        };

        // Act
        await _service.SyncMember(keycloakId);

        // Assert
        var updatedMember = await _db.Members.FirstAsync(m => m.Id == member.Id);
        Assert.Equal("newbob@example.com", updatedMember.Email);

        var tasks = await _db.MailSubscriptionOutboxTasks.ToListAsync();
        Assert.Single(tasks);
        Assert.Contains(tasks, t => t.TaskType == MailSubscriptionOutboxTaskType.MigrateEmail
            && t.OldEmail == "bob@example.com" && t.Email == "newbob@example.com");
    }

    [Fact]
    public async Task CreateUser_Success_ReturnsKeycloakId()
    {
        // Arrange
        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Charlie",
            LastName = "Brown",
            Email = "charlie@example.com",
            StudentNumber = "s3333333",
            PhoneNumber = "+31600000000",
            Street = "St",
            HouseNumber = "1",
            PostalCode = "1234AB",
            City = "Enschede"
        };

        var keycloakId = Guid.NewGuid();

        _adminHandler.SendAsyncFunc = (req) =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.EndsWith("users", req.RequestUri?.AbsolutePath);

            var resp = new HttpResponseMessage(HttpStatusCode.Created);
            resp.Headers.Location = new Uri($"https://keycloak.local/admin/realms/tavern/users/{keycloakId}");
            return Task.FromResult(resp);
        };

        // Act
        var result = await _service.CreateUser(member);

        // Assert
        Assert.Equal(keycloakId, result);
    }

    [Fact]
    public async Task DeleteUser_Success_CompletesSuccessfully()
    {
        // Arrange
        var keycloakId = Guid.NewGuid();

        _adminHandler.SendAsyncFunc = (req) =>
        {
            Assert.Equal(HttpMethod.Delete, req.Method);
            Assert.EndsWith($"users/{keycloakId}", req.RequestUri?.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        };

        // Act
        await _service.DeleteUser(keycloakId);

        // Assert
        // Verified by handler returning success
    }

    [Fact]
    public async Task GetEmail_Success_ReturnsEmailAddress()
    {
        // Arrange
        var keycloakId = Guid.NewGuid();

        _adminHandler.SendAsyncFunc = (req) =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.EndsWith($"users/{keycloakId}", req.RequestUri?.AbsolutePath);

            var userResponse = new { email = "dan@example.com" };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(userResponse))
            });
        };

        // Act
        var result = await _service.GetEmail(keycloakId);

        // Assert
        Assert.Equal("dan@example.com", result);
    }

    [Fact]
    public async Task RefreshEmail_Success_UpdatesDatabase()
    {
        // Arrange
        var keycloakId = Guid.NewGuid();
        var member = new Member
        {
            Id = Guid.NewGuid(),
            AuthSystemUserId = keycloakId,
            FirstName = "Eve",
            LastName = "White",
            Email = "eve@example.com",
            StudentNumber = "s4444444",
            PhoneNumber = "+31600000000",
            Street = "St",
            HouseNumber = "1",
            PostalCode = "1234AB",
            City = "Enschede"
        };
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _adminHandler.SendAsyncFunc = (req) =>
        {
            var userResponse = new { email = "neweve@example.com" };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(userResponse))
            });
        };

        // Act
        await _service.RefreshEmail(keycloakId);

        // Assert
        var updated = await _db.Members.FirstAsync(m => m.Id == member.Id);
        Assert.Equal("neweve@example.com", updated.Email);
    }

    [Fact]
    public async Task GetServiceAccountToken_Failure_ThrowsException()
    {
        // Arrange
        _tokenHandler.SendAsyncFunc = (req) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("invalid_client")
        });

        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Frank",
            LastName = "Miller",
            Email = "frank@example.com",
            StudentNumber = "s5555555",
            PhoneNumber = "+31600000000",
            Street = "St",
            HouseNumber = "1",
            PostalCode = "1234AB",
            City = "Enschede"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _service.CreateUser(member));
        Assert.Contains("Keycloak Auth Failed", ex.Message);
    }

    [Fact]
    public async Task CreateUser_Failure_ThrowsException()
    {
        // Arrange
        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Grace",
            LastName = "Hopper",
            Email = "grace@example.com",
            StudentNumber = "s6666666",
            PhoneNumber = "+31600000000",
            Street = "St",
            HouseNumber = "1",
            PostalCode = "1234AB",
            City = "Enschede"
        };

        _adminHandler.SendAsyncFunc = (req) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("invalid user")
        });

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _service.CreateUser(member));
    }

    [Fact]
    public async Task SendActivationEmail_Success_CompletesSuccessfully()
    {
        // Arrange
        var keycloakId = Guid.NewGuid();

        _adminHandler.SendAsyncFunc = (req) =>
        {
            Assert.Equal(HttpMethod.Put, req.Method);
            Assert.EndsWith($"users/{keycloakId}/execute-actions-email", req.RequestUri?.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        };

        // Act
        await _service.SendActivationEmail(keycloakId);

        // Assert
        // Verified by handler returning success
    }

    [Fact]
    public async Task SendActivationEmail_Failure_ThrowsException()
    {
        // Arrange
        var keycloakId = Guid.NewGuid();

        _adminHandler.SendAsyncFunc = (req) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("cannot send email")
        });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _service.SendActivationEmail(keycloakId));
        Assert.Contains("Keycloak Email Failed", ex.Message);
    }

    [Fact]
    public async Task GetEmail_Failure_ThrowsException()
    {
        // Arrange
        var keycloakId = Guid.NewGuid();

        _adminHandler.SendAsyncFunc = (req) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("user not found")
        });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _service.GetEmail(keycloakId));
        Assert.Contains("Keycloak User Fetch Failed", ex.Message);
    }
}
