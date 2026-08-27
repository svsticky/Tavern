using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Backend.Database;
using Backend.Models.Domain;
using Backend.Services.MailSubscriptionServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Backend.Tests.Services;

public class MailChimpSubscriptionServiceTests : IDisposable
{
    private readonly DbContextOptions<PostgresDbContext> _dbOptions;
    private readonly MockHttpMessageHandler _httpHandler;
    private readonly HttpClient _httpClient;

    public MailChimpSubscriptionServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _httpHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri("https://api.mailchimp.com/3.0/")
        };
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("MAIL_SUBSCRIPTION_SERVICE", null);
        Environment.SetEnvironmentVariable("MAILCHIMP_LIST_KEY", null);
        _httpClient.Dispose();
    }

    public class MockHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? SendAsyncFunc { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (SendAsyncFunc == null)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
            return SendAsyncFunc(request, cancellationToken);
        }
    }

    private PostgresDbContext CreateEnabledDb()
    {
        var db = new PostgresDbContext(_dbOptions);
        db.Settings.Add(new Setting { Name = "MailSubscriptionService", Value = "MAILCHIMP" });
        db.Settings.Add(new Setting { Name = "MailchimpListKey", Value = "test_list_123" });
        db.SaveChanges();
        return db;
    }

    /// Two interest categories, each with a couple of interests, matching what
    /// GetAvailableMailinglistsAsync fetches (categories, then interests per category).
    private void SetupAvailableListsHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? extra = null)
    {
        _httpHandler.SendAsyncFunc = (req, ct) =>
        {
            var path = req.RequestUri!.AbsolutePath;

            if (path.EndsWith("interest-categories"))
            {
                var body = new { categories = new[] { new { id = "cat_1" } } };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(body)
                });
            }

            if (path.EndsWith("interest-categories/cat_1/interests"))
            {
                var body = new
                {
                    interests = new[]
                    {
                        new { id = "id_news", name = "Newsletter" },
                        new { id = "id_events", name = "Events" },
                        new { id = "id_career", name = "Career" }
                    }
                };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(body)
                });
            }

            if (extra != null)
            {
                return extra(req, ct);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        };
    }

    [Fact]
    public async Task GetAvailableMailinglistsAsync_ServiceDisabled_ReturnsEmpty()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        var service = new MailChimpSubscriptionService(NullLogger<MailChimpSubscriptionService>.Instance, _httpClient, db);

        bool httpCalled = false;
        _httpHandler.SendAsyncFunc = (req, ct) => { httpCalled = true; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)); };

        // Act
        var result = await service.GetAvailableMailinglistsAsync(CancellationToken.None);

        // Assert
        Assert.Empty(result);
        Assert.False(httpCalled);
    }

    [Fact]
    public async Task GetAvailableMailinglistsAsync_ServiceEnabled_ReturnsFlattenedInterests()
    {
        // Arrange
        using var db = CreateEnabledDb();
        var service = new MailChimpSubscriptionService(NullLogger<MailChimpSubscriptionService>.Instance, _httpClient, db);
        SetupAvailableListsHandler();

        // Act
        var result = (await service.GetAvailableMailinglistsAsync(CancellationToken.None)).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, l => l.Id == "id_news" && l.Name == "Newsletter");
        Assert.Contains(result, l => l.Id == "id_events" && l.Name == "Events");
        Assert.Contains(result, l => l.Id == "id_career" && l.Name == "Career");
    }

    [Fact]
    public async Task GetAvailableMailinglistsAsync_ApiKeyConfigured_ConfiguresHttpClient()
    {
        // Arrange
        using var db = CreateEnabledDb();
        db.Settings.Add(new Setting { Name = "MailchimpApiKey", Value = "abc123-us1" });
        db.SaveChanges();

        var httpClient = new HttpClient(_httpHandler);
        var service = new MailChimpSubscriptionService(NullLogger<MailChimpSubscriptionService>.Instance, httpClient, db);
        SetupAvailableListsHandler();

        // Act
        await service.GetAvailableMailinglistsAsync(CancellationToken.None);

        // Assert
        Assert.Equal(new Uri("https://us1.api.mailchimp.com/3.0/"), httpClient.BaseAddress);
        Assert.NotNull(httpClient.DefaultRequestHeaders.Authorization);
        Assert.Equal("Basic", httpClient.DefaultRequestHeaders.Authorization!.Scheme);
    }

    [Fact]
    public async Task GetMemberMailinglistsAsync_ServiceDisabled_ReturnsEmpty()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        var service = new MailChimpSubscriptionService(NullLogger<MailChimpSubscriptionService>.Instance, _httpClient, db);

        bool httpCalled = false;
        _httpHandler.SendAsyncFunc = (req, ct) => { httpCalled = true; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)); };

        // Act
        var result = await service.GetMemberMailinglistsAsync("someone@example.com", CancellationToken.None);

        // Assert
        Assert.Empty(result);
        Assert.False(httpCalled);
    }

    [Fact]
    public async Task GetMemberMailinglistsAsync_NotFound_ReturnsAllUnsubscribed()
    {
        // Arrange
        using var db = CreateEnabledDb();
        var service = new MailChimpSubscriptionService(NullLogger<MailChimpSubscriptionService>.Instance, _httpClient, db);
        SetupAvailableListsHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        // Act
        var result = (await service.GetMemberMailinglistsAsync("test@example.com", CancellationToken.None)).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(result, l => Assert.False(l.Subscribed));
    }

    [Fact]
    public async Task GetMemberMailinglistsAsync_StatusUnsubscribed_IgnoresStaleInterests()
    {
        // Arrange - Mailchimp does not clear `interests` when a member unsubscribes, so a
        // non-"subscribed" status must not resurrect stale opt-ins.
        using var db = CreateEnabledDb();
        var service = new MailChimpSubscriptionService(NullLogger<MailChimpSubscriptionService>.Instance, _httpClient, db);
        SetupAvailableListsHandler((req, ct) =>
        {
            var body = new { status = "unsubscribed", interests = new Dictionary<string, bool> { { "id_news", true }, { "id_events", true } } };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) });
        });

        // Act
        var result = (await service.GetMemberMailinglistsAsync("test@example.com", CancellationToken.None)).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(result, l => Assert.False(l.Subscribed));
    }

    [Fact]
    public async Task GetMemberMailinglistsAsync_StatusCleaned_IgnoresStaleInterests()
    {
        // Arrange
        using var db = CreateEnabledDb();
        var service = new MailChimpSubscriptionService(NullLogger<MailChimpSubscriptionService>.Instance, _httpClient, db);
        SetupAvailableListsHandler((req, ct) =>
        {
            var body = new { status = "cleaned", interests = new Dictionary<string, bool> { { "id_news", true } } };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) });
        });

        // Act
        var result = (await service.GetMemberMailinglistsAsync("test@example.com", CancellationToken.None)).ToList();

        // Assert
        Assert.All(result, l => Assert.False(l.Subscribed));
    }

    [Fact]
    public async Task GetMemberMailinglistsAsync_StatusSubscribed_ReflectsInterests()
    {
        // Arrange
        using var db = CreateEnabledDb();
        var service = new MailChimpSubscriptionService(NullLogger<MailChimpSubscriptionService>.Instance, _httpClient, db);
        SetupAvailableListsHandler((req, ct) =>
        {
            var body = new { status = "subscribed", interests = new Dictionary<string, bool> { { "id_news", true }, { "id_events", false } } };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) });
        });

        // Act
        var result = (await service.GetMemberMailinglistsAsync("test@example.com", CancellationToken.None)).ToList();

        // Assert
        Assert.True(result.Single(l => l.Id == "id_news").Subscribed);
        Assert.False(result.Single(l => l.Id == "id_events").Subscribed);
        Assert.False(result.Single(l => l.Id == "id_career").Subscribed); // absent from interests dict
    }

    [Fact]
    public async Task UpdateMemberSubscriptionsAsync_ServiceDisabled_DoesNothing()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        var service = new MailChimpSubscriptionService(NullLogger<MailChimpSubscriptionService>.Instance, _httpClient, db);

        bool httpCalled = false;
        _httpHandler.SendAsyncFunc = (req, ct) => { httpCalled = true; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)); };

        // Act
        await service.UpdateMemberSubscriptionsAsync("test@example.com", ["id_news"], CancellationToken.None);

        // Assert
        Assert.False(httpCalled);
    }

    [Fact]
    public async Task UpdateMemberSubscriptionsAsync_EmptySelection_ArchivesMember()
    {
        // Arrange
        using var db = CreateEnabledDb();
        var service = new MailChimpSubscriptionService(NullLogger<MailChimpSubscriptionService>.Instance, _httpClient, db);

        HttpRequestMessage? receivedRequest = null;
        _httpHandler.SendAsyncFunc = (req, ct) =>
        {
            receivedRequest = req;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        };

        // Act
        await service.UpdateMemberSubscriptionsAsync("test@example.com", [], CancellationToken.None);

        // Assert
        Assert.NotNull(receivedRequest);
        Assert.Equal(HttpMethod.Delete, receivedRequest.Method);
        // MD5 of "test@example.com" is "55502f40dc8b7c769880b10874abc9d0"
        Assert.Contains("lists/test_list_123/members/55502f40dc8b7c769880b10874abc9d0", receivedRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task UpdateMemberSubscriptionsAsync_NonEmptySelection_SendsPutRequestWithInterests()
    {
        // Arrange
        using var db = CreateEnabledDb();
        var service = new MailChimpSubscriptionService(NullLogger<MailChimpSubscriptionService>.Instance, _httpClient, db);

        HttpRequestMessage? receivedRequest = null;
        SetupAvailableListsHandler((req, ct) =>
        {
            receivedRequest = req;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        // Act
        await service.UpdateMemberSubscriptionsAsync("test@example.com", ["id_news", "id_events"], CancellationToken.None);

        // Assert
        Assert.NotNull(receivedRequest);
        Assert.Equal(HttpMethod.Put, receivedRequest.Method);
        Assert.Contains("lists/test_list_123/members/55502f40dc8b7c769880b10874abc9d0", receivedRequest.RequestUri!.ToString());

        var contentString = await receivedRequest.Content!.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(contentString);
        var root = jsonDoc.RootElement;

        Assert.Equal("test@example.com", root.GetProperty("email_address").GetString());
        Assert.Equal("subscribed", root.GetProperty("status").GetString());

        var interests = root.GetProperty("interests");
        Assert.True(interests.GetProperty("id_news").GetBoolean());
        Assert.True(interests.GetProperty("id_events").GetBoolean());
        Assert.False(interests.GetProperty("id_career").GetBoolean());
    }

    [Fact]
    public async Task DeleteMemberAsync_ServiceDisabled_DoesNothing()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        var service = new MailChimpSubscriptionService(NullLogger<MailChimpSubscriptionService>.Instance, _httpClient, db);

        bool httpCalled = false;
        _httpHandler.SendAsyncFunc = (req, ct) => { httpCalled = true; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)); };

        // Act
        await service.DeleteMemberAsync("test@example.com", CancellationToken.None);

        // Assert
        Assert.False(httpCalled);
    }

    [Fact]
    public async Task DeleteMemberAsync_ServiceEnabled_SendsDeleteRequest()
    {
        // Arrange
        using var db = CreateEnabledDb();
        var service = new MailChimpSubscriptionService(NullLogger<MailChimpSubscriptionService>.Instance, _httpClient, db);

        HttpRequestMessage? receivedRequest = null;
        _httpHandler.SendAsyncFunc = (req, ct) =>
        {
            receivedRequest = req;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        };

        // Act
        await service.DeleteMemberAsync("test@example.com", CancellationToken.None);

        // Assert
        Assert.NotNull(receivedRequest);
        Assert.Equal(HttpMethod.Delete, receivedRequest.Method);
        Assert.Contains("lists/test_list_123/members/55502f40dc8b7c769880b10874abc9d0", receivedRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task DeleteMemberAsync_NotFoundStatus_DoesNotThrow()
    {
        // Arrange
        using var db = CreateEnabledDb();
        var service = new MailChimpSubscriptionService(NullLogger<MailChimpSubscriptionService>.Instance, _httpClient, db);
        _httpHandler.SendAsyncFunc = (req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act & Assert (should not throw)
        await service.DeleteMemberAsync("test@example.com", CancellationToken.None);
    }

    [Fact]
    public async Task MigrateEmailAsync_ServiceDisabled_DoesNothing()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        var service = new MailChimpSubscriptionService(NullLogger<MailChimpSubscriptionService>.Instance, _httpClient, db);

        bool httpCalled = false;
        _httpHandler.SendAsyncFunc = (req, ct) => { httpCalled = true; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)); };

        // Act
        await service.MigrateEmailAsync("old@example.com", "new@example.com", CancellationToken.None);

        // Assert
        Assert.False(httpCalled);
    }

    [Fact]
    public async Task MigrateEmailAsync_FetchesOldSubscriptions_PushesToNewEmail_ArchivesOld()
    {
        // Arrange
        using var db = CreateEnabledDb();
        var service = new MailChimpSubscriptionService(NullLogger<MailChimpSubscriptionService>.Instance, _httpClient, db);

        // MD5("old@example.com")
        var oldHash = "bf25d950bde50b8e13f413bb4eb0b1dd";

        var putRequests = new List<HttpRequestMessage>();
        var deleteRequests = new List<HttpRequestMessage>();

        _httpHandler.SendAsyncFunc = (req, ct) =>
        {
            var path = req.RequestUri!.AbsolutePath;

            if (path.EndsWith("interest-categories"))
            {
                var body = new { categories = new[] { new { id = "cat_1" } } };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) });
            }

            if (path.EndsWith("interest-categories/cat_1/interests"))
            {
                var body = new { interests = new[] { new { id = "id_news", name = "Newsletter" } } };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) });
            }

            if (req.Method == HttpMethod.Get && path.EndsWith($"members/{oldHash}"))
            {
                var body = new { status = "subscribed", interests = new Dictionary<string, bool> { { "id_news", true } } };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) });
            }

            if (req.Method == HttpMethod.Put)
            {
                putRequests.Add(req);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }

            if (req.Method == HttpMethod.Delete)
            {
                deleteRequests.Add(req);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        };

        // Act
        await service.MigrateEmailAsync("old@example.com", "new@example.com", CancellationToken.None);

        // Assert
        Assert.Single(putRequests);
        var putContent = await putRequests[0].Content!.ReadAsStringAsync();
        Assert.Contains("new@example.com", putContent);

        Assert.Single(deleteRequests);
        Assert.Contains($"members/{oldHash}", deleteRequests[0].RequestUri!.ToString());
    }
}
