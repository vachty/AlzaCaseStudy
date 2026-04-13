using AggregationService.Application.Caching;
using AggregationService.Application.Connector;
using AggregationService.Application.Contracts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AggregationService.Application.Services
{
    /// <summary>
    /// The service for product aggregation
    /// </summary>
    public class ProductAggregationService(
            IProductServiceClient productServiceClient,
            IPricingServiceClient pricingServiceClient,
            IStockServiceClient stockServiceClient,
            ILogger<ProductAggregationService> logger,
            AggregatedProductMemoryCache memoryCache)
    {
        /// <summary>
        /// Gets the aggregated product
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<AggregatedProductDto?> GetByIdAsync(string productId, CancellationToken cancellationToken = default)
        {
            return await memoryCache.GetOrCreateAsync(
                productId,
                async ct =>
                {
                    var degraded = new List<string>();

                    var productTask = SafeExecuteAsync(
                        () => productServiceClient.GetProductAsync(productId, ct),
                        "ProductService",
                        degraded);

                    var pricingTask = SafeExecuteAsync(
                        () => pricingServiceClient.GetPriceAsync(productId, ct),
                        "PricingService",
                        degraded);

                    var stockTask = SafeExecuteAsync(
                        () => stockServiceClient.GetStockAsync(productId, ct),
                        "StockService",
                        degraded);

                    await Task.WhenAll(productTask, pricingTask, stockTask);

                    var product = await productTask;
                    var price = await pricingTask;
                    var stock = await stockTask;

                    if (product is null)
                    {
                        logger.LogWarning(
                            "Unable to aggregate product {ProductId} because product data is missing.",
                            productId);

                        return null;
                    }

                    return new AggregatedProductDto
                    {
                        ProductId = product.Id,
                        Name = product.Name,
                        ImageUrl = product.ImageUrl,
                        Price = price?.Amount,
                        Currency = price?.Currency,
                        IsAvailable = stock?.InStock ?? false,
                        StockQuantity = stock?.Quantity,
                        Degraded = degraded
                    };
                },
                cancellationToken);
        }

        /// <summary>
        /// Executes the given action
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <param name="dependencyName"></param>
        /// <param name="degraded"></param>
        /// <returns></returns>
        private async Task<T?> SafeExecuteAsync<T>(
            Func<Task<T?>> action,
            string dependencyName,
            List<string> degraded)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Dependency {DependencyName} failed. Falling back to partial response.",
                    dependencyName);

                degraded.Add(dependencyName);
                return default;
            }
        }
    }
}
