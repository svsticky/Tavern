using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Backend.Database;
using Backend.Models.Domain;
using Backend.Services.MailSubscriptionServices;
using Backend.Services.OutboxWorkers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Backend.Tests.Services;

public class ListmonkSubscriptionServiceTests : IDisposable
{
    private readonly DbContextOptions<PostgresDbContext> _dbOptions;
    private readonly MailChimpSubscriptionServiceTests.MockHttpMessageHandler _httpHandler;
    private readonly HttpClient _httpClient;
    private readonly MailSubscriptionOutboxWorker _mailWorker;
    private readonly List<HttpRequestMessage> _requests = [];

    public ListmonkSubscriptionServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _httpHandler = new MailChimpSubscriptionServiceTests.MockHttpMessageHandler();
        _httpClient = new HttpClient(_httpHandler);
        _mailWorker = new MailSubscriptionOutboxWorker(null!, NullLogger<MailSubscriptionOutboxWorker>.Instance);
    }

    public void Dispose() => _httpClient.Dispose();

    private ListmonkSubscriptionService CreateService()
    {
        var db = new PostgresDbContext(_dbOptions);
        db.Settings.Add(new Setting { Name = "MailSubscriptionService", Value = "LISTMONK" });
        db.Settings.Add(new Setting { Name = "ListmonkApiKey", Value = "key" });
        db.Settings.Add(new Setting { Name = "ListmonkUser", Value = "user" });
        db.Settings.Add(new Setting { Name = "ListmonkUrl", Value = "https://listmonk.test/" });
        db.SaveChanges();
        return new ListmonkSubscriptionService(NullLogger<ListmonkSubscriptionService>.Instance, _httpClient, db, _mailWorker);
    }

    /// <summary>
    /// Fakes Listmonk: the subscriber lookup returns <paramref name="existingId"/> (or nothing when null),
    /// every other request succeeds. All requests are recorded in <see cref="_requests"/>.
    /// </summary>
    private void SetupListmonk(int? existingId, HttpStatusCode deleteStatus = HttpStatusCode.OK)
    {
        _httpHandler.SendAsyncFunc = (req, ct) =>
        {
            _requests.Add(req);

            if (req.Method == HttpMethod.Get && req.RequestUri!.AbsolutePath == "/api/subscribers")
            {
                object[] results = existingId is int id ? [new { id, lists = Array.Empty<object>() }] : [];
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { data = new { results } })
                });
            }

            if (req.Method == HttpMethod.Delete)
                return Task.FromResult(new HttpResponseMessage(deleteStatus));

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        };
    }

    private IEnumerable<HttpRequestMessage> Writes => _requests.Where(r => r.Method != HttpMethod.Get);

    [Fact]
    public async Task DeleteMemberAsync_SubscriberNotFound_DoesNothing()
    {
        SetupListmonk(existingId: null);

        await CreateService().DeleteMemberAsync("ghost@example.com", CancellationToken.None);

        Assert.Empty(Writes);
    }

    [Fact]
    public async Task DeleteMemberAsync_SubscriberExists_DeletesIt()
    {
        SetupListmonk(existingId: 42);

        await CreateService().DeleteMemberAsync("member@example.com", CancellationToken.None);

        var request = Assert.Single(Writes);
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal("/api/subscribers/42", request.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task DeleteMemberAsync_DeleteReturnsNotFound_DoesNotThrow()
    {
        SetupListmonk(existingId: 42, deleteStatus: HttpStatusCode.NotFound);

        await CreateService().DeleteMemberAsync("member@example.com", CancellationToken.None);
    }

    [Fact]
    public async Task DeleteMemberAsync_DeleteFails_Throws()
    {
        SetupListmonk(existingId: 42, deleteStatus: HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            CreateService().DeleteMemberAsync("member@example.com", CancellationToken.None));
    }

    [Fact]
    public async Task UpdateMemberSubscriptionsAsync_EmptySelection_SubscriberExists_DeletesIt()
    {
        SetupListmonk(existingId: 42);

        await CreateService().UpdateMemberSubscriptionsAsync("member@example.com", [], CancellationToken.None);

        var request = Assert.Single(Writes);
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal("/api/subscribers/42", request.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task UpdateMemberSubscriptionsAsync_EmptySelection_SubscriberNotFound_DoesNotCreate()
    {
        SetupListmonk(existingId: null);

        await CreateService().UpdateMemberSubscriptionsAsync("new@example.com", [], CancellationToken.None);

        Assert.Empty(Writes);
    }

    [Fact]
    public async Task UpdateMemberSubscriptionsAsync_NonEmptySelection_SubscriberNotFound_Creates()
    {
        SetupListmonk(existingId: null);

        await CreateService().UpdateMemberSubscriptionsAsync("new@example.com", ["1", "2"], CancellationToken.None);

        var request = Assert.Single(Writes);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/subscribers", request.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task UpdateMemberSubscriptionsAsync_NonEmptySelection_SubscriberExists_Patches()
    {
        SetupListmonk(existingId: 42);

        await CreateService().UpdateMemberSubscriptionsAsync("member@example.com", ["1"], CancellationToken.None);

        var request = Assert.Single(Writes);
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal("/api/subscribers/42", request.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task UpdateMemberNameAsync_SubscriberNotFound_DoesNothing()
    {
        SetupListmonk(existingId: null);

        await CreateService().UpdateMemberNameAsync("ghost@example.com", "First", "Last", CancellationToken.None);

        Assert.Empty(Writes);
    }

    [Fact]
    public async Task MigrateEmailAsync_OldSubscriberNotFound_DoesNotCreateNewOne()
    {
        SetupListmonk(existingId: null);

        await CreateService().MigrateEmailAsync("old@example.com", "new@example.com", CancellationToken.None);

        Assert.Empty(Writes);
    }

    [Fact]
    public async Task MigrateEmailAsync_OldSubscriberExists_PatchesEmail()
    {
        SetupListmonk(existingId: 42);

        await CreateService().MigrateEmailAsync("old@example.com", "new@example.com", CancellationToken.None);

        var request = Assert.Single(Writes);
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal("/api/subscribers/42", request.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Lookup_EmailWithPlusAndQuote_IsEscapedInQuery()
    {
        SetupListmonk(existingId: null);

        await CreateService().DeleteMemberAsync("o'brien+tag@example.com", CancellationToken.None);

        var lookup = Assert.Single(_requests);
        var query = Uri.UnescapeDataString(lookup.RequestUri!.Query);
        Assert.Contains("LOWER('o''brien+tag@example.com')", query);
    }
}
