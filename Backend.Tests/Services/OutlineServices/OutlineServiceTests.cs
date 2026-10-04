using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Backend.Database;
using Backend.Models.Domain;
using Backend.Services.OutlineServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Backend.Tests.Services.OutlineServices;

public class OutlineServiceTests : IDisposable
{
    private readonly DbContextOptions<PostgresDbContext> _dbOptions;
    private readonly MockHttpMessageHandler _httpHandler;
    private readonly HttpClient _httpClient;

    public OutlineServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _httpHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_httpHandler);
    }

    public void Dispose()
    {
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

    private PostgresDbContext CreateDb(bool enabled = true, string apiUrl = "https://outline.svsticky.nl", string apiKey = "test_token", string? boardGroup = null)
    {
        var db = new PostgresDbContext(_dbOptions);
        db.Settings.Add(new Setting { Name = "OutlineEnabled", Value = enabled ? "true" : "false" });
        db.Settings.Add(new Setting { Name = "OutlineApiUrl", Value = apiUrl });
        db.Settings.Add(new Setting { Name = "OutlineApiKey", Value = apiKey });
        if (boardGroup != null)
        {
            db.Settings.Add(new Setting { Name = "OutlineBoardGroupName", Value = boardGroup });
        }
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task SyncMailAsync_WhenDisabled_DoesNotMakeAnyHttpCalls()
    {
        // Arrange
        using var db = CreateDb(enabled: false);
        bool called = false;
        _httpHandler.SendAsyncFunc = (req, ct) =>
        {
            called = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        };

        var service = new OutlineService(NullLogger<OutlineService>.Instance, _httpClient, db);

        // Act
        await service.SyncMailAsync("old@example.com", "new@example.com", CancellationToken.None);

        // Assert
        Assert.False(called);
    }

    [Fact]
    public async Task SyncMailAsync_WhenUserNotFoundInOutline_DoesNotThrowAndSkipsUpdate()
    {
        // Arrange
        using var db = CreateDb(enabled: true);
        var requests = new List<HttpRequestMessage>();
        _httpHandler.SendAsyncFunc = (req, ct) =>
        {
            requests.Add(req);
            if (req.RequestUri!.AbsolutePath.EndsWith("api/users.list"))
            {
                var body = new { data = Array.Empty<object>() };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(body)
                });
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        };

        var service = new OutlineService(NullLogger<OutlineService>.Instance, _httpClient, db);

        // Act
        await service.SyncMailAsync("unknown@example.com", "new@example.com", CancellationToken.None);

        // Assert
        Assert.Single(requests);
        Assert.EndsWith("api/users.list", requests[0].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task SyncMailAsync_WhenUserFound_SendsUpdateEmailRequest()
    {
        // Arrange
        using var db = CreateDb(enabled: true);
        var requests = new List<HttpRequestMessage>();
        string? updatePayload = null;

        _httpHandler.SendAsyncFunc = async (req, ct) =>
        {
            requests.Add(req);
            var path = req.RequestUri!.AbsolutePath;

            if (path.EndsWith("api/users.list"))
            {
                var body = new
                {
                    data = new[]
                    {
                        new { id = "user_abc", name = "Test User", email = "old@example.com", role = "member" }
                    }
                };
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(body)
                };
            }

            if (path.EndsWith("api/users.updateEmail"))
            {
                updatePayload = await req.Content!.ReadAsStringAsync(ct);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { success = true })
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        };

        var service = new OutlineService(NullLogger<OutlineService>.Instance, _httpClient, db);

        // Act
        await service.SyncMailAsync("old@example.com", "new@example.com", CancellationToken.None);

        // Assert
        Assert.Equal(2, requests.Count);
        Assert.NotNull(updatePayload);
        var doc = JsonDocument.Parse(updatePayload);
        Assert.Equal("user_abc", doc.RootElement.GetProperty("id").GetString());
        Assert.Equal("new@example.com", doc.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task SyncMailAsync_WhenUpdateEmailReturns404_HandlesGracefullyWithoutThrowing()
    {
        // Arrange
        using var db = CreateDb(enabled: true);
        _httpHandler.SendAsyncFunc = (req, ct) =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.EndsWith("api/users.list"))
            {
                var body = new
                {
                    data = new[]
                    {
                        new { id = "user_deleted", name = "Test", email = "user@example.com", role = "member" }
                    }
                };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(body)
                });
            }

            if (path.EndsWith("api/users.updateEmail"))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        };

        var service = new OutlineService(NullLogger<OutlineService>.Instance, _httpClient, db);

        // Act & Assert (should not throw)
        await service.SyncMailAsync("user@example.com", "new@example.com", CancellationToken.None);
    }

    [Fact]
    public async Task SyncMailAsync_WhenUpdateEmailFailsWith500_ThrowsHttpRequestException()
    {
        // Arrange
        using var db = CreateDb(enabled: true);
        _httpHandler.SendAsyncFunc = (req, ct) =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.EndsWith("api/users.list"))
            {
                var body = new
                {
                    data = new[]
                    {
                        new { id = "user_1", name = "Test", email = "user@example.com", role = "member" }
                    }
                };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(body)
                });
            }

            if (path.EndsWith("api/users.updateEmail"))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        };

        var service = new OutlineService(NullLogger<OutlineService>.Instance, _httpClient, db);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => service.SyncMailAsync("user@example.com", "new@example.com", CancellationToken.None));
    }

    [Fact]
    public async Task DeleteMailAsync_WhenDisabled_DoesNotMakeAnyHttpCalls()
    {
        // Arrange
        using var db = CreateDb(enabled: false);
        bool called = false;
        _httpHandler.SendAsyncFunc = (req, ct) =>
        {
            called = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        };

        var service = new OutlineService(NullLogger<OutlineService>.Instance, _httpClient, db);

        // Act
        await service.DeleteMailAsync("user@example.com", CancellationToken.None);

        // Assert
        Assert.False(called);
    }

    [Fact]
    public async Task DeleteMailAsync_WhenUserNotFoundInOutline_DoesNotThrowAndSkipsDelete()
    {
        // Arrange
        using var db = CreateDb(enabled: true);
        var requests = new List<HttpRequestMessage>();
        _httpHandler.SendAsyncFunc = (req, ct) =>
        {
            requests.Add(req);
            var body = new { data = Array.Empty<object>() };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(body)
            });
        };

        var service = new OutlineService(NullLogger<OutlineService>.Instance, _httpClient, db);

        // Act
        await service.DeleteMailAsync("notfound@example.com", CancellationToken.None);

        // Assert
        Assert.Single(requests);
    }

    [Fact]
    public async Task DeleteMailAsync_WhenUserFound_SendsDeleteRequest()
    {
        // Arrange
        using var db = CreateDb(enabled: true);
        var requests = new List<HttpRequestMessage>();
        string? deletePayload = null;

        _httpHandler.SendAsyncFunc = async (req, ct) =>
        {
            requests.Add(req);
            var path = req.RequestUri!.AbsolutePath;

            if (path.EndsWith("api/users.list"))
            {
                var body = new
                {
                    data = new[]
                    {
                        new { id = "user_to_delete", name = "Test", email = "delete@example.com", role = "member" }
                    }
                };
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(body)
                };
            }

            if (path.EndsWith("api/users.delete"))
            {
                deletePayload = await req.Content!.ReadAsStringAsync(ct);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { success = true })
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        };

        var service = new OutlineService(NullLogger<OutlineService>.Instance, _httpClient, db);

        // Act
        await service.DeleteMailAsync("delete@example.com", CancellationToken.None);

        // Assert
        Assert.Equal(2, requests.Count);
        Assert.NotNull(deletePayload);
        var doc = JsonDocument.Parse(deletePayload);
        Assert.Equal("user_to_delete", doc.RootElement.GetProperty("id").GetString());
    }

    [Fact]
    public async Task DeleteMailAsync_WhenDeleteReturns404_HandlesGracefully()
    {
        // Arrange
        using var db = CreateDb(enabled: true);
        _httpHandler.SendAsyncFunc = (req, ct) =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.EndsWith("api/users.list"))
            {
                var body = new
                {
                    data = new[]
                    {
                        new { id = "user_gone", name = "Test", email = "delete@example.com", role = "member" }
                    }
                };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(body)
                });
            }

            if (path.EndsWith("api/users.delete"))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        };

        var service = new OutlineService(NullLogger<OutlineService>.Instance, _httpClient, db);

        // Act & Assert (no exception)
        await service.DeleteMailAsync("delete@example.com", CancellationToken.None);
    }

    [Fact]
    public async Task UpdateAdminStatusAsync_WhenDisabled_DoesNotMakeAnyHttpCalls()
    {
        // Arrange
        using var db = CreateDb(enabled: false);
        bool called = false;
        _httpHandler.SendAsyncFunc = (req, ct) =>
        {
            called = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        };

        var service = new OutlineService(NullLogger<OutlineService>.Instance, _httpClient, db);

        // Act
        await service.UpdateAdminStatusAsync("board@example.com", true, CancellationToken.None);

        // Assert
        Assert.False(called);
    }

    [Fact]
    public async Task UpdateAdminStatusAsync_WhenUserNotFound_SkipsGracefully()
    {
        // Arrange
        using var db = CreateDb(enabled: true);
        var requests = new List<HttpRequestMessage>();
        _httpHandler.SendAsyncFunc = (req, ct) =>
        {
            requests.Add(req);
            var body = new { data = Array.Empty<object>() };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(body)
            });
        };

        var service = new OutlineService(NullLogger<OutlineService>.Instance, _httpClient, db);

        // Act
        await service.UpdateAdminStatusAsync("notfound@example.com", true, CancellationToken.None);

        // Assert
        Assert.Single(requests);
    }

    [Fact]
    public async Task UpdateAdminStatusAsync_PromoteAdmin_WhenGroupExists_PromotesRoleAndAddsToGroup()
    {
        // Arrange
        using var db = CreateDb(enabled: true);
        var requests = new List<HttpRequestMessage>();
        string? rolePayload = null;
        string? addPayload = null;

        _httpHandler.SendAsyncFunc = async (req, ct) =>
        {
            requests.Add(req);
            var path = req.RequestUri!.AbsolutePath;

            if (path.EndsWith("api/users.list"))
            {
                var body = new
                {
                    data = new[]
                    {
                        new { id = "user_board", name = "Board Member", email = "board@example.com", role = "member" }
                    }
                };
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) };
            }

            if (path.EndsWith("api/users.update_role"))
            {
                rolePayload = await req.Content!.ReadAsStringAsync(ct);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { success = true }) };
            }

            if (path.EndsWith("api/groups.list"))
            {
                var body = new
                {
                    data = new
                    {
                        groups = new[]
                        {
                            new { id = "group_board_id", name = "Board" }
                        }
                    }
                };
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) };
            }

            if (path.EndsWith("api/groups.add_user"))
            {
                addPayload = await req.Content!.ReadAsStringAsync(ct);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { success = true }) };
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        };

        var service = new OutlineService(NullLogger<OutlineService>.Instance, _httpClient, db);

        // Act
        await service.UpdateAdminStatusAsync("board@example.com", true, CancellationToken.None);

        // Assert
        Assert.NotNull(rolePayload);
        var roleDoc = JsonDocument.Parse(rolePayload);
        Assert.Equal("user_board", roleDoc.RootElement.GetProperty("id").GetString());
        Assert.Equal("admin", roleDoc.RootElement.GetProperty("role").GetString());

        Assert.NotNull(addPayload);
        var addDoc = JsonDocument.Parse(addPayload);
        Assert.Equal("group_board_id", addDoc.RootElement.GetProperty("id").GetString());
        Assert.Equal("user_board", addDoc.RootElement.GetProperty("userId").GetString());
    }

    [Fact]
    public async Task UpdateAdminStatusAsync_PromoteAdmin_WhenGroupDoesNotExist_CreatesGroupAndAddsUser()
    {
        // Arrange
        using var db = CreateDb(enabled: true);
        var requests = new List<HttpRequestMessage>();
        string? createPayload = null;
        string? addPayload = null;

        _httpHandler.SendAsyncFunc = async (req, ct) =>
        {
            requests.Add(req);
            var path = req.RequestUri!.AbsolutePath;

            if (path.EndsWith("api/users.list"))
            {
                var body = new
                {
                    data = new[]
                    {
                        new { id = "user_new_board", name = "New Board", email = "newboard@example.com", role = "member" }
                    }
                };
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) };
            }

            if (path.EndsWith("api/users.update_role"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { success = true }) };
            }

            if (path.EndsWith("api/groups.list"))
            {
                var body = new { data = new { groups = Array.Empty<object>() } };
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) };
            }

            if (path.EndsWith("api/groups.create"))
            {
                createPayload = await req.Content!.ReadAsStringAsync(ct);
                var body = new
                {
                    data = new { id = "created_group_id", name = "Board" }
                };
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) };
            }

            if (path.EndsWith("api/groups.add_user"))
            {
                addPayload = await req.Content!.ReadAsStringAsync(ct);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { success = true }) };
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        };

        var service = new OutlineService(NullLogger<OutlineService>.Instance, _httpClient, db);

        // Act
        await service.UpdateAdminStatusAsync("newboard@example.com", true, CancellationToken.None);

        // Assert
        Assert.NotNull(createPayload);
        var createDoc = JsonDocument.Parse(createPayload);
        Assert.Equal("Board", createDoc.RootElement.GetProperty("name").GetString());

        Assert.NotNull(addPayload);
        var addDoc = JsonDocument.Parse(addPayload);
        Assert.Equal("created_group_id", addDoc.RootElement.GetProperty("id").GetString());
        Assert.Equal("user_new_board", addDoc.RootElement.GetProperty("userId").GetString());
    }

    [Fact]
    public async Task UpdateAdminStatusAsync_DemoteAdmin_WhenGroupExists_DemotesRoleAndRemovesFromGroup()
    {
        // Arrange
        using var db = CreateDb(enabled: true);
        var requests = new List<HttpRequestMessage>();
        string? rolePayload = null;
        string? removePayload = null;

        _httpHandler.SendAsyncFunc = async (req, ct) =>
        {
            requests.Add(req);
            var path = req.RequestUri!.AbsolutePath;

            if (path.EndsWith("api/users.list"))
            {
                var body = new
                {
                    data = new[]
                    {
                        new { id = "user_old_board", name = "Former Board", email = "former@example.com", role = "admin" }
                    }
                };
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) };
            }

            if (path.EndsWith("api/users.update_role"))
            {
                rolePayload = await req.Content!.ReadAsStringAsync(ct);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { success = true }) };
            }

            if (path.EndsWith("api/groups.list"))
            {
                var body = new
                {
                    data = new
                    {
                        groups = new[]
                        {
                            new { id = "group_board_id", name = "Board" }
                        }
                    }
                };
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) };
            }

            if (path.EndsWith("api/groups.remove_user"))
            {
                removePayload = await req.Content!.ReadAsStringAsync(ct);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { success = true }) };
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        };

        var service = new OutlineService(NullLogger<OutlineService>.Instance, _httpClient, db);

        // Act
        await service.UpdateAdminStatusAsync("former@example.com", false, CancellationToken.None);

        // Assert
        Assert.NotNull(rolePayload);
        var roleDoc = JsonDocument.Parse(rolePayload);
        Assert.Equal("user_old_board", roleDoc.RootElement.GetProperty("id").GetString());
        Assert.Equal("member", roleDoc.RootElement.GetProperty("role").GetString());

        Assert.NotNull(removePayload);
        var removeDoc = JsonDocument.Parse(removePayload);
        Assert.Equal("group_board_id", removeDoc.RootElement.GetProperty("id").GetString());
        Assert.Equal("user_old_board", removeDoc.RootElement.GetProperty("userId").GetString());
    }

    [Fact]
    public async Task UpdateAdminStatusAsync_WhenAddUserReturnsBadRequestOrNotFound_HandlesGracefully()
    {
        // Arrange
        using var db = CreateDb(enabled: true);
        _httpHandler.SendAsyncFunc = (req, ct) =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.EndsWith("api/users.list"))
            {
                var body = new
                {
                    data = new[]
                    {
                        new { id = "u_1", name = "Test", email = "test@example.com", role = "member" }
                    }
                };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) });
            }
            if (path.EndsWith("api/users.update_role"))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { success = true }) });
            }
            if (path.EndsWith("api/groups.list"))
            {
                var body = new
                {
                    data = new { groups = new[] { new { id = "g_1", name = "Board" } } }
                };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) });
            }
            if (path.EndsWith("api/groups.add_user"))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        };

        var service = new OutlineService(NullLogger<OutlineService>.Instance, _httpClient, db);

        // Act & Assert (should not throw)
        await service.UpdateAdminStatusAsync("test@example.com", true, CancellationToken.None);
    }
}
