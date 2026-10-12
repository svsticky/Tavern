using System.Text.Json;
using Backend.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Backend.Tests.Services;

public class CacheServiceTests
{
    private readonly IDistributedCache _cacheMock;
    private readonly CacheService _service;

    public CacheServiceTests()
    {
        _cacheMock = Substitute.For<IDistributedCache>();
        _service = new CacheService(_cacheMock, NullLogger<CacheService>.Instance);
    }

    private class SamplePayload
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAsync_InvalidKey_ThrowsArgumentException(string? invalidKey)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            _service.GetAsync<SamplePayload>(invalidKey!));
    }

    [Fact]
    public async Task GetAsync_KeyNotFound_ReturnsDefault()
    {
        _cacheMock.GetAsync("missing_key", Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        var result = await _service.GetAsync<SamplePayload>("missing_key");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_ValidCachedJson_ReturnsDeserializedObject()
    {
        var expected = new SamplePayload { Id = 42, Name = "Baco Event" };
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(expected);

        _cacheMock.GetAsync("sample_key", Arg.Any<CancellationToken>())
            .Returns(jsonBytes);

        var result = await _service.GetAsync<SamplePayload>("sample_key");

        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal("Baco Event", result.Name);
    }

    [Fact]
    public async Task GetAsync_CacheThrowsException_LogsAndReturnsDefault()
    {
        _cacheMock.GetAsync("err_key", Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Redis unavailable"));

        var result = await _service.GetAsync<SamplePayload>("err_key");

        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SetAsync_InvalidKey_ThrowsArgumentException(string? invalidKey)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            _service.SetAsync(invalidKey!, new SamplePayload()));
    }

    [Fact]
    public async Task SetAsync_ValidObject_StoresInDistributedCache()
    {
        var item = new SamplePayload { Id = 10, Name = "Airco Rush" };

        await _service.SetAsync("peak_key", item, TimeSpan.FromMinutes(10));

        await _cacheMock.Received(1).SetAsync(
            "peak_key",
            Arg.Is<byte[]>(bytes => bytes.Length > 0),
            Arg.Is<DistributedCacheEntryOptions>(opt => opt.AbsoluteExpirationRelativeToNow == TimeSpan.FromMinutes(10)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetAsync_CacheThrowsException_LogsAndDoesNotThrow()
    {
        _cacheMock.SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Connection failed"));

        var exception = await Record.ExceptionAsync(() =>
            _service.SetAsync("err_key", new SamplePayload()));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RemoveAsync_InvalidKey_ThrowsArgumentException(string? invalidKey)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            _service.RemoveAsync(invalidKey!));
    }

    [Fact]
    public async Task RemoveAsync_ValidKey_CallsCacheRemove()
    {
        await _service.RemoveAsync("item_to_remove");

        await _cacheMock.Received(1).RemoveAsync("item_to_remove", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveAsync_CacheThrowsException_LogsAndDoesNotThrow()
    {
        _cacheMock.RemoveAsync("err_key", Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Redis down"));

        var exception = await Record.ExceptionAsync(() =>
            _service.RemoveAsync("err_key"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task GetOrSetAsync_NullFactory_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.GetOrSetAsync<SamplePayload>("key", null!));
    }

    [Fact]
    public async Task GetOrSetAsync_KeyInCache_ReturnsCachedWithoutCallingFactory()
    {
        var cachedItem = new SamplePayload { Id = 1, Name = "Cached" };
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(cachedItem);

        _cacheMock.GetAsync("cached_key", Arg.Any<CancellationToken>())
            .Returns(jsonBytes);

        var factoryCalled = false;
        var result = await _service.GetOrSetAsync("cached_key", () =>
        {
            factoryCalled = true;
            return Task.FromResult(new SamplePayload { Id = 2, Name = "Fresh" });
        });

        Assert.False(factoryCalled);
        Assert.Equal("Cached", result.Name);
    }

    [Fact]
    public async Task GetOrSetAsync_KeyNotInCache_InvokesFactoryAndCachesResult()
    {
        _cacheMock.GetAsync("new_key", Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        var factoryCalled = false;
        var freshItem = new SamplePayload { Id = 99, Name = "Freshly Computed" };

        var result = await _service.GetOrSetAsync("new_key", () =>
        {
            factoryCalled = true;
            return Task.FromResult(freshItem);
        }, TimeSpan.FromMinutes(2));

        Assert.True(factoryCalled);
        Assert.Equal(99, result.Id);
        await _cacheMock.Received(1).SetAsync(
            "new_key",
            Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }
}
