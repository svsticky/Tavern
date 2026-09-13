namespace Backend.Interfaces;

/// <summary>
/// Provides caching operations backed by a distributed cache (such as Redis) with memory fallback.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Retrieves a cached item by key.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The cached value, or null if not found.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    /// <summary>
    /// Stores an item in the cache with an optional expiration time.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="expiration">The time to live. If null, a default 5-minute expiration is applied.</param>
    /// <param name="ct">The cancellation token.</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default);

    /// <summary>
    /// Removes an item from the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="ct">The cancellation token.</param>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a cached item, or generates and stores it if it does not exist.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">The asynchronous factory function to produce the item when absent from cache.</param>
    /// <param name="expiration">The time to live.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The cached or freshly generated value.</returns>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken ct = default);
}
