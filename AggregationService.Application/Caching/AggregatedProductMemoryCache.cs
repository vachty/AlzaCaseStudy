using AggregationService.Application.Contracts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace AggregationService.Application.Caching;

public class AggregatedProductMemoryCache(
    IMemoryCache memoryCache,
    ILogger<AggregatedProductMemoryCache> logger)
{
    private const int SuccessCacheDurationSeconds = 30;
    private const int DegradedCacheDurationSeconds = 10;

    private readonly ConcurrentDictionary<string, SemaphoreSlim> locks = new();

    /// <summary>
    /// Gets or creates an aggregated product in the cache
    /// </summary>
    /// <param name="productId"></param>
    /// <param name="factory"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<AggregatedProductDto?> GetOrCreateAsync(
        string productId,
        Func<CancellationToken, Task<AggregatedProductDto?>> factory,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(productId);

        if (memoryCache.TryGetValue(cacheKey, out AggregatedProductDto? cachedProduct))
        {
            logger.LogInformation("Cache hit for product {ProductId}", productId);
            return cachedProduct;
        }

        logger.LogInformation("Cache miss for product {ProductId}", productId);

        var semaphore = locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            if (memoryCache.TryGetValue(cacheKey, out cachedProduct))
            {
                logger.LogInformation(
                    "Cache hit for product {ProductId} after waiting for lock",
                    productId);

                return cachedProduct;
            }

            var aggregatedProduct = await factory(cancellationToken);

            if (aggregatedProduct is null)
            {
                logger.LogWarning(
                    "Factory returned null for product {ProductId}. Nothing will be cached.",
                    productId);

                return null;
            }

            var cacheDurationSeconds = aggregatedProduct.Degraded.Count == 0
                ? SuccessCacheDurationSeconds
                : DegradedCacheDurationSeconds;

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(cacheDurationSeconds)
            };

            memoryCache.Set(cacheKey, aggregatedProduct, cacheOptions);

            logger.LogInformation(
                "Cached aggregated product {ProductId} for {CacheDurationSeconds} seconds",
                productId,
                cacheDurationSeconds);

            return aggregatedProduct;
        }
        finally
        {
            semaphore.Release();

            if (semaphore.CurrentCount == 1)
            {
                locks.TryRemove(cacheKey, out _);
            }
        }
    }

    /// <summary>
    /// Builds the cache key
    /// </summary>
    /// <param name="productId"></param>
    /// <returns></returns>
    private static string BuildCacheKey(string productId)
        => $"aggregated-product:{productId}";
}
