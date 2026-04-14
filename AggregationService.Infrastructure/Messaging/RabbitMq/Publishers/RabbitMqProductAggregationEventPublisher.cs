using AggregationService.Application.Configuration;
using AggregationService.Application.Events;
using AggregationService.Application.Publishers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace AggregationService.Infrastructure.Messaging.RabbitMq.Publishers;

/// <summary>
/// The publisher for product aggregation events using RabbitMQ
/// </summary>
public sealed class RabbitMqProductAggregationEventPublisher : IProductAggregationEventPublisher, IDisposable
{
    private readonly ILogger<RabbitMqProductAggregationEventPublisher> _logger;
    private readonly MessagingOptions _options;
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    /// <summary>
    /// .ctor
    /// </summary>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    public RabbitMqProductAggregationEventPublisher(
        IOptions<MessagingOptions> options,
        ILogger<RabbitMqProductAggregationEventPublisher> logger)
    {
        _logger = logger;
        _options = options.Value;

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public async Task PublishAsync(ProductAggregatedEvent productAggregatedEvent, CancellationToken ct = default)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(productAggregatedEvent));

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json"
        };

        await _channel.BasicPublishAsync(
            exchange: _options.ExchangeName,
            routingKey: _options.RoutingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: ct);

        _logger.LogInformation(
            "Published product aggregated event for product {ProductId} to exchange {ExchangeName} with routing key {RoutingKey}",
            productAggregatedEvent.ProductId,
            _options.ExchangeName,
            _options.RoutingKey);
    }

    /// <summary>
    /// Disposes the publisher
    /// </summary>
    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}
