using AggregationService.Application.Connector;
using AggregationService.Domain;
using System.Net.Http.Json;

namespace AggregationService.Infrastructure.Clients;

public class StockServiceClient : IStockServiceClient
{
    private readonly HttpClient _httpClient;

    public StockServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<StockInfo?> GetStockAsync(string productId, CancellationToken ct = default)
        => await _httpClient.GetFromJsonAsync<StockInfo>($"/api/stock/{productId}", ct);
}