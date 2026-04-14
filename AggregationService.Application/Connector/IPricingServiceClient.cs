using AggregationService.Domain;

namespace AggregationService.Application.Connector;

public interface IPricingServiceClient
{
    Task<Price?> GetPriceAsync(string productId, CancellationToken ct = default);
}
