namespace AggregationService.API.Configuration;

public class ServicesOptions
{
    public const string SectionName = nameof(ServicesOptions);

    public string ProductSimulationUrl { get; set; } = string.Empty;
    public string PricingSimulationUrl { get; set; } = string.Empty;
    public string StockSimulationUrl { get; set; } = string.Empty;
}