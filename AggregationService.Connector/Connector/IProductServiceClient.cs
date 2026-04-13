using AggregationService.Domain;

namespace AggregationService.Application.Connector;

public interface IProductServiceClient
{
    Task<Product?> GetProductAsync(string productId, CancellationToken ct = default);
}
