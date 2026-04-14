namespace AggregationService.Application.Events;

/// <summary>
/// The event representing the result of product aggregation
/// </summary>
public sealed record ProductAggregatedEvent
{
    public string ProductId { get; init; } = default!;
    public DateTime OccurredAtUtc { get; init; }
    public bool IsDegraded { get; init; }
    public List<string> DegradedServices { get; init; } = [];
}