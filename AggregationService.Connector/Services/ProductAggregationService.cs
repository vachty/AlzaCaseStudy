using AggregationService.Application.Connector;
using AggregationService.Application.Contracts;
using Microsoft.Extensions.Logging;

namespace AggregationService.Application.Services
{
    /// <summary>
    /// The service for product aggregation
    /// </summary>
    public class ProductAggregationService
    {
        private readonly IProductServiceClient _productServiceClient;
        private readonly IPricingServiceClient _pricingServiceClient;
        private readonly IStockServiceClient _stockServiceClient;
        private readonly ILogger<ProductAggregationService> _logger;

        public ProductAggregationService(
            IProductServiceClient productServiceClient,
            IPricingServiceClient pricingServiceClient,
            IStockServiceClient stockServiceClient,
            ILogger<ProductAggregationService> logger)
        {
            _productServiceClient = productServiceClient;
            _pricingServiceClient = pricingServiceClient;
            _stockServiceClient = stockServiceClient;
            _logger = logger;
        }

        /// <summary>
        /// Gets the aggregated product
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<AggregatedProductDto?> GetByIdAsync(string productId, CancellationToken cancellationToken = default)
        {
            var degraded = new List<string>();

            var productTask = SafeExecuteAsync(
                () => _productServiceClient.GetProductAsync(productId, cancellationToken),
                "ProductService",
                degraded);

            var pricingTask = SafeExecuteAsync(
                () => _pricingServiceClient.GetPriceAsync(productId, cancellationToken),
                "PricingService",
                degraded);

            var stockTask = SafeExecuteAsync(
                () => _stockServiceClient.GetStockAsync(productId, cancellationToken),
                "StockService",
                degraded);

            await Task.WhenAll(productTask, pricingTask, stockTask);

            var product = await productTask;
            var price = await pricingTask;
            var stock = await stockTask;

            // product is the most important, if its null then skip the rest
            if (product is null)
            {
                _logger.LogWarning(
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
                _logger.LogWarning(
                    ex,
                    "Dependency {DependencyName} failed. Falling back to partial response.",
                    dependencyName);

                degraded.Add(dependencyName);
                return default;
            }
        }
    }
}
