namespace AggregationService.Application.Contracts;

/// <summary>
/// The contract / dto for product aggregation
/// </summary>
public record AggregatedProductDto
{
    public string ProductId { get; init; } = default!;
    public string? Name { get; init; }
    public string? ImageUrl { get; init; }
    public decimal? Price { get; init; }
    public string? Currency { get; init; }
    public bool IsAvailable { get; init; }
    public int? StockQuantity { get; init; }

    /// <summary>
    /// Indicates which downstream services failed (partial response).
    /// </summary>
    public List<string> Degraded { get; init; } = [];
}
