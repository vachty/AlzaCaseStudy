using FluentAssertions;
using System.Net;
using Xunit;

namespace AggregationService.IntegrationTests;

/// <summary>
/// Tests for the product aggregation endpoint
/// </summary>
public sealed class ProductAggregationEndpointTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public ProductAggregationEndpointTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetById_ShouldReturnOk()
    {
        // Act
        var response = await _fixture.Client.GetAsync("/api/v1/products/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
