using AggregationService.API.Configuration;
using AggregationService.Application.Connector;
using AggregationService.Application.Services;
using AggregationService.Infrastructure.Clients;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace AggregationService
{
    public static class ServicesExtensions
    {
        /// <summary>
        /// Adds the serilog
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static WebApplicationBuilder AddSerilog(this WebApplicationBuilder builder)
        {
            builder.Host.UseSerilog((ctx, config) => 
                config.ReadFrom.Configuration(ctx.Configuration));

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
            services.AddScoped<ProductAggregationService>();

            return services;
        }
    }
}
