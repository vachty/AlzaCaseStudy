using AggregationService.Application.Caching;
using AggregationService.Application.Connector;
using AggregationService.Application.Publishers;
using AggregationService.Application.Services;
using AggregationService.Domain;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace AggregationService.Tests.Services;

public class ProductAggregationServiceTests
{
    private readonly Mock<IProductServiceClient> productServiceClient = new();
    private readonly Mock<IPricingServiceClient> pricingServiceClient = new();
    private readonly Mock<IStockServiceClient> stockServiceClient = new();
    private readonly Mock<ILogger<ProductAggregationService>> serviceLogger = new();
    private readonly Mock<ILogger<AggregatedProductMemoryCache>> cacheLogger = new();
    private readonly Mock<IProductAggregationEventPublisher> eventPublisher = new();

    /// <summary>
    /// Creates the ProductAggregationService
    /// </summary>
    /// <param name="memoryCache"></param>
    /// <returns></returns>
    private ProductAggregationService CreateService(IMemoryCache? memoryCache = null)
    {
        memoryCache ??= new MemoryCache(new MemoryCacheOptions());

        var aggregatedProductMemoryCache = new AggregatedProductMemoryCache(
            memoryCache,
            cacheLogger.Object);

        return new ProductAggregationService(
            productServiceClient.Object,
            pricingServiceClient.Object,
            stockServiceClient.Object,
            serviceLogger.Object,
            aggregatedProductMemoryCache,
            eventPublisher.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnAggregatedProduct_WhenAllServicesSucceed()
    {
        // Arrange
        const string productId = "1";

        productServiceClient
            .Setup(x => x.GetProductAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Product(productId, "iPhone 15", "https://example.com/iphone.jpg"));

        pricingServiceClient
            .Setup(x => x.GetPriceAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Price(25990m, "CZK"));

        stockServiceClient
            .Setup(x => x.GetStockAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StockInfo(productId, 12, true));

        var aggregationService = CreateService();

        // Act
        var result = await aggregationService.GetByIdAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result!.ProductId.Should().Be(productId);
        result.Name.Should().Be("iPhone 15");
        result.ImageUrl.Should().Be("https://example.com/iphone.jpg");
        result.Price.Should().Be(25990m);
        result.Currency.Should().Be("CZK");
        result.IsAvailable.Should().BeTrue();
        result.StockQuantity.Should().Be(12);
        result.Degraded.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnPartialResponse_WhenPricingFails()
    {
        // Arrange
        const string productId = "1";

        productServiceClient
            .Setup(x => x.GetProductAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Product(productId, "iPhone 15", "https://example.com/iphone.jpg"));

        pricingServiceClient
            .Setup(x => x.GetPriceAsync(productId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Pricing failed"));

        stockServiceClient
            .Setup(x => x.GetStockAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StockInfo(productId, 12, true));

        var aggregationService = CreateService();

        // Act
        var result = await aggregationService.GetByIdAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result!.ProductId.Should().Be(productId);
        result.Price.Should().BeNull();
        result.Currency.Should().BeNull();
        result.IsAvailable.Should().BeTrue();
        result.StockQuantity.Should().Be(12);
        result.Degraded.Should().ContainSingle("PricingService");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnPartialResponse_WhenStockFails()
    {
        // Arrange
        const string productId = "1";

        productServiceClient
            .Setup(x => x.GetProductAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Product(productId, "iPhone 15", "https://example.com/iphone.jpg"));

        pricingServiceClient
            .Setup(x => x.GetPriceAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Price(25990m, "CZK"));

        stockServiceClient
            .Setup(x => x.GetStockAsync(productId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Stock failed"));

        var aggregationService = CreateService();

        // Act
        var result = await aggregationService.GetByIdAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result!.ProductId.Should().Be(productId);
        result.Price.Should().Be(25990m);
        result.Currency.Should().Be("CZK");
        result.IsAvailable.Should().BeFalse();
        result.StockQuantity.Should().BeNull();
        result.Degraded.Should().ContainSingle("StockService");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenProductIsMissing()
    {
        // Arrange
        const string productId = "999";

        productServiceClient
            .Setup(x => x.GetProductAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        pricingServiceClient
            .Setup(x => x.GetPriceAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Price(25990m, "CZK"));

        stockServiceClient
            .Setup(x => x.GetStockAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StockInfo(productId, 12, true));

        var aggregationService = CreateService();

        // Act
        var result = await aggregationService.GetByIdAsync(productId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldUseCache_OnSecondCall()
    {
        // Arrange
        const string productId = "1";
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());

        productServiceClient
            .Setup(x => x.GetProductAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Product(productId, "iPhone 15", "https://example.com/iphone.jpg"));

        pricingServiceClient
            .Setup(x => x.GetPriceAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Price(25990m, "CZK"));

        stockServiceClient
            .Setup(x => x.GetStockAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StockInfo(productId, 12, true));

        var aggregationService = CreateService(memoryCache);

        // Act
        var first = await aggregationService.GetByIdAsync(productId);
        var second = await aggregationService.GetByIdAsync(productId);

        // Assert
        second.Should().NotBeNull();
        second.Should().BeEquivalentTo(first);

        productServiceClient.Verify(
            x => x.GetProductAsync(productId, It.IsAny<CancellationToken>()),
            Times.Once);

        pricingServiceClient.Verify(
            x => x.GetPriceAsync(productId, It.IsAny<CancellationToken>()),
            Times.Once);

        stockServiceClient.Verify(
            x => x.GetStockAsync(productId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}