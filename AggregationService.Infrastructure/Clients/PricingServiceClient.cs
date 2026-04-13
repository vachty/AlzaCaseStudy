using AggregationService.Application.Connector;
using AggregationService.Domain;
using System.Net.Http.Json;

namespace AggregationService.Infrastructure.Clients;

public class PricingServiceClient : IPricingServiceClient
{
    private readonly HttpClient _httpClient;

    public PricingServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Price?> GetPriceAsync(string productId, CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync<Price>($"/api/prices/{productId}", ct);
    }
}