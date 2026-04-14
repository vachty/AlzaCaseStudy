using AggregationService.API.Configuration;
using AggregationService.Application.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AggregationService.IntegrationTests;

/// <summary>
/// The custom web application factory
/// </summary>
/// <param name="productServiceUrl"></param>
/// <param name="pricingServiceUrl"></param>
/// <param name="stockServiceUrl"></param>
/// <param name="rabbitMqHost"></param>
/// <param name="rabbitMqPort"></param>
public sealed class CustomWebApplicationFactory(
    string productServiceUrl,
    string pricingServiceUrl,
    string stockServiceUrl,
    string rabbitMqHost,
    ushort rabbitMqPort) : WebApplicationFactory<Program>
{
    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                [$"{ServicesOptions.SectionName}:ProductSimulationUrl"] = productServiceUrl,
                [$"{ServicesOptions.SectionName}:PricingSimulationUrl"] = pricingServiceUrl,
                [$"{ServicesOptions.SectionName}:StockSimulationUrl"] = stockServiceUrl,
                [$"{MessagingOptions.SectionName}:HostName"] = rabbitMqHost,
                [$"{MessagingOptions.SectionName}:Port"] = rabbitMqPort.ToString(),
                [$"{MessagingOptions.SectionName}:UserName"] = "guest",
                [$"{MessagingOptions.SectionName}:Password"] = "guest",
                [$"{MessagingOptions.SectionName}:ExchangeName"] = "product-events",
                [$"{MessagingOptions.SectionName}:RoutingKey"] = "product.aggregated"
            };

            configBuilder.AddInMemoryCollection(overrides);
        });
    }
}
