using Backend.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Backend.Services;

/// <summary>
/// Implements distributed caching operations using <see cref="IDistributedCache"/> (Redis/Memory)
/// with graceful error handling and JSON serialization.
/// </summary>
public class CacheService(IDistributedCache cache, ILogger<CacheService> logger) : ICacheService
{
    private readonly IDistributedCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly ILogger<CacheService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var bytes = await _cache.GetAsync(key, ct);
            if (bytes == null || bytes.Length == 0)
                return default;

            return JsonSerializer.Deserialize<T>(bytes, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read key {CacheKey} from distributed cache. Falling back to source.", key);
            return default;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _jsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5)
            };

            await _cache.SetAsync(key, bytes, options, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write key {CacheKey} to distributed cache.", key);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            await _cache.RemoveAsync(key, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove key {CacheKey} from distributed cache.", key);
        }
    }

    /// <inheritdoc />
    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);

        var cached = await GetAsync<T>(key, ct);
        if (cached != null)
            return cached;

        var fresh = await factory();

        if (fresh != null)
        {
            await SetAsync(key, fresh, expiration, ct);
        }

        return fresh;
    }
}
