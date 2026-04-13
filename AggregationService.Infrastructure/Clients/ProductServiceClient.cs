using AggregationService.Application.Connector;
using AggregationService.Domain;
using System.Net.Http.Json;

namespace AggregationService.Infrastructure.Clients;

public class ProductServiceClient : IProductServiceClient
{
    private readonly HttpClient _httpClient;

    public ProductServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Product?> GetProductAsync(string productId, CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync<Product>($"/api/products/{productId}", ct);
    }
}