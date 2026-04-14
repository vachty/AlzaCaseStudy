namespace AggregationService.Application.Configuration;

/// <summary>
/// The options for async messaging
/// </summary>
public class MessagingOptions
{
    public const string SectionName = nameof(MessagingOptions);

    public string HostName { get; init; } = "rabbitmq";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string ExchangeName { get; init; } = "product-events";
    public string RoutingKey { get; init; } = "product.aggregated";
}
