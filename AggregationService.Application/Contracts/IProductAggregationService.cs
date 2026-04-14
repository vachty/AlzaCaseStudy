namespace AggregationService.Application.Contracts;

/// <summary>
/// The product aggregation service interface
/// </summary>
public interface IProductAggregationService
{
    /// <summary>
    /// Gets an aggregated product by its identifier
    /// </summary>
    /// <param name="productId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<AggregatedProductDto?> GetByIdAsync(string productId, CancellationToken ct = default);
}
