using System;
using System.Collections.Generic;
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

    [Fact]
    public async Task UpdateSubscriptionAsync_ServiceDisabled_DoesNothing()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MAIL_SUBSCRIPTION_SERVICE", "DISABLED");
        using var db = new PostgresDbContext(_dbOptions);
        var service = new MailChimpSubscriptionService(
            NullLogger<MailChimpSubscriptionService>.Instance,
            _httpClient,
            db
        );

        bool httpCalled = false;
        _httpHandler.SendAsyncFunc = (req, ct) =>
        {
            httpCalled = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        };

        // Act
        await service.UpdateSubscriptionAsync("test@example.com", 3u, CancellationToken.None);

        // Assert
        Assert.False(httpCalled);
    }

    [Fact]
    public async Task UpdateSubscriptionAsync_ServiceEnabled_MailSubscriptionZero_SendsDeleteRequest()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Settings.Add(new Setting { Name = "MailSubscriptionService", Value = "MAILCHIMP" });
        db.Settings.Add(new Setting { Name = "MailchimpListKey", Value = "test_list_123" });
        db.SaveChanges();

        var service = new MailChimpSubscriptionService(
            NullLogger<MailChimpSubscriptionService>.Instance,
            _httpClient,
            db
        );

        HttpRequestMessage? receivedRequest = null;
        _httpHandler.SendAsyncFunc = (req, ct) =>
        {
            receivedRequest = req;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        };

        // Act
        await service.UpdateSubscriptionAsync("test@example.com", 0u, CancellationToken.None);

        // Assert
        Assert.NotNull(receivedRequest);
        Assert.Equal(HttpMethod.Delete, receivedRequest.Method);
        // MD5 of "test@example.com" is "55502f40dc8b7c769880b10874abc9d0"
        Assert.Contains("lists/test_list_123/members/55502f40dc8b7c769880b10874abc9d0", receivedRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task UpdateSubscriptionAsync_ServiceEnabled_MailSubscriptionZero_NotFoundStatus_DoesNotThrow()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MAIL_SUBSCRIPTION_SERVICE", "MAILCHIMP");
        Environment.SetEnvironmentVariable("MAILCHIMP_LIST_KEY", "test_list_123");
        using var db = new PostgresDbContext(_dbOptions);
        var service = new MailChimpSubscriptionService(
            NullLogger<MailChimpSubscriptionService>.Instance,
            _httpClient,
            db
        );

        _httpHandler.SendAsyncFunc = (req, ct) =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        };

        // Act & Assert (should not throw when MailChimp returns NotFound)
        await service.UpdateSubscriptionAsync("test@example.com", 0u, CancellationToken.None);
    }

    [Fact]
    public async Task UpdateSubscriptionAsync_ServiceEnabled_MailSubscriptionNonZero_SendsPutRequestWithInterests()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();
        db.Settings.Add(new Setting { Name = "MailSubscriptionService", Value = "MAILCHIMP" });
        db.Settings.Add(new Setting { Name = "MailchimpListKey", Value = "test_list_123" });
        db.Mailinglists.Add(new Mailinglist { Id = 1, Name = "Newsletter", BitValue = 1u, ServiceId = "id_news" });
        db.Mailinglists.Add(new Mailinglist { Id = 2, Name = "Events", BitValue = 2u, ServiceId = "id_events" });
        db.Mailinglists.Add(new Mailinglist { Id = 3, Name = "Career", BitValue = 4u, ServiceId = "id_career" });
        await db.SaveChangesAsync();

        var service = new MailChimpSubscriptionService(
            NullLogger<MailChimpSubscriptionService>.Instance,
            _httpClient,
            db
        );

        HttpRequestMessage? receivedRequest = null;
        _httpHandler.SendAsyncFunc = (req, ct) =>
        {
            receivedRequest = req;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        };

        // Act
        // Subscription = 3u (binary 011), meaning subscribed to Newsletter (1) and Events (2), but not Career (4)
        await service.UpdateSubscriptionAsync("test@example.com", 3u, CancellationToken.None);

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
}
