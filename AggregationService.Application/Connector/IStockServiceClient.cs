using AggregationService.Domain;

namespace AggregationService.Application.Connector;

public interface IStockServiceClient
{
    Task<StockInfo?> GetStockAsync(string productId, CancellationToken ct = default);
}
