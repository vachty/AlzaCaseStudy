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
public sealed class RabbitMqProductAggregationEventPublisher(ILogger<RabbitMqProductAggregationEventPublisher> logger, IOptions<MessagingOptions> options) : IProductAggregationEventPublisher, IDisposable
{
    public IConnection? Connection { get; private set; } 
    public IChannel? Channel { get; private set; }

    /// <inheritdoc/>
    public async Task PublishAsync(ProductAggregatedEvent evt, CancellationToken ct = default)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = options.Value.HostName,
                    Port = options.Value.Port,
                    UserName = options.Value.UserName,
                    Password = options.Value.Password,
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
                    SocketReadTimeout = TimeSpan.FromSeconds(5),
                    SocketWriteTimeout = TimeSpan.FromSeconds(5)
                };

                await using var connection = await factory.CreateConnectionAsync(ct);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

                await channel.ExchangeDeclareAsync(
                    exchange: options.Value.ExchangeName,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false,
                    cancellationToken: ct);

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));

                var properties = new BasicProperties
                {
                    Persistent = true,
                    ContentType = "application/json"
                };

                await channel.BasicPublishAsync(
                    exchange: options.Value.ExchangeName,
                    routingKey: options.Value.RoutingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: body,
                    cancellationToken: ct);

                return;
            }
            catch (Exception ex) when (attempt < 5)
            {
                lastException = ex;
                await Task.Delay(500, ct);
            }
        }

        throw lastException!;
    }

    /// <summary>
    /// Disposes the publisher
    /// </summary>
    public void Dispose()
    {
        Channel?.Dispose();
        Connection?.Dispose();
    }
}
