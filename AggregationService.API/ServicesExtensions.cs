using AggregationService.API.Configuration;
using AggregationService.Application.Connector;
using AggregationService.Application.Contracts;
using AggregationService.Application.Services;
using AggregationService.Infrastructure.Clients;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Serilog;
using Serilog.Events;

namespace AggregationService;

public static class ServicesExtensions
{
    /// <summary>
    /// Adds the serilog
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) => configuration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] [CorrelationId: {CorrelationId}] {SourceContext} {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: "/app/logs/aggregation-service-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                shared: true,
                outputTemplate:
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [CorrelationId: {CorrelationId}] {SourceContext} {Message:lj}{NewLine}{Exception}"));

        return builder;
    }

    /// <summary>
    /// Adds the open telemetry for tracing and metrics
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddTelemetry(this IServiceCollection services) 
    {
        services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService("AggregationService"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddConsoleExporter())
            .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter());

        return services;
    }

    /// <summary>
    /// Adds the options
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection AddServicesOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ServicesOptions>(configuration.GetSection(ServicesOptions.SectionName));
        return services;
    }

    /// <summary>
    /// Adds the HTTP clients for the downstream services (product, pricing, stock)
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddClients(this IServiceCollection services) 
    {
        // product client
        services.AddHttpClient<IProductServiceClient, ProductServiceClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ServicesOptions>>().Value;
            client.BaseAddress = new Uri(options.ProductSimulationUrl);
        });

        // pricing client
        services.AddHttpClient<IPricingServiceClient, PricingServiceClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ServicesOptions>>().Value;
            client.BaseAddress = new Uri(options.PricingSimulationUrl);
        })
        .AddResilienceHandler("pricing-pipeline", pipeline =>
        {
            pipeline.AddTimeout(TimeSpan.FromSeconds(2));

            pipeline.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromMilliseconds(250),
                BackoffType = DelayBackoffType.Exponential
            });

            pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                SamplingDuration = TimeSpan.FromSeconds(10),
                FailureRatio = 0.5,
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(15)
            });
        });

        // stock client
        services.AddHttpClient<IStockServiceClient, StockServiceClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ServicesOptions>>().Value;
            client.BaseAddress = new Uri(options.StockSimulationUrl);
        });

        return services;
    }

    /// <summary>
    /// Adds the services
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IProductAggregationService, ProductAggregationService>();
        return services;
    }
}
