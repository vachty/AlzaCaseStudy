using AggregationService.Application.Events;

namespace AggregationService.Application.Publishers;

/// <summary>
/// Publishes product aggregation integration events
/// </summary>
public interface IProductAggregationEventPublisher
{
    /// <summary>
    /// Publishes the event
    /// </summary>
    /// <param name="productAggregatedEvent"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task PublishAsync(ProductAggregatedEvent productAggregatedEvent, CancellationToken ct = default);
}
