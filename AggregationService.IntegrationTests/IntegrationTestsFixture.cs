using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using Testcontainers.RabbitMq;
using Xunit;

namespace AggregationService.IntegrationTests;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    private readonly IFutureDockerImage _productImage;
    private readonly IFutureDockerImage _pricingImage;
    private readonly IFutureDockerImage _stockImage;

    private readonly IContainer _productContainer;
    private readonly IContainer _pricingContainer;
    private readonly IContainer _stockContainer;
    private readonly IContainer _rabbitMqContainer;

    public CustomWebApplicationFactory Factory { get; private set; } = default!;
    public HttpClient Client { get; private set; } = default!;

    public string ProductServiceUrl => $"http://localhost:{_productContainer.GetMappedPublicPort(8080)}";
    public string PricingServiceUrl => $"http://localhost:{_pricingContainer.GetMappedPublicPort(8080)}";
    public string StockServiceUrl => $"http://localhost:{_stockContainer.GetMappedPublicPort(8080)}";

    public string RabbitMqHost => "localhost";
    public ushort RabbitMqPort => (ushort)_rabbitMqContainer.GetMappedPublicPort(5672);

    /// <summary>
    /// .ctor
    /// </summary>
    public IntegrationTestFixture()
    {
        var solutionDirectory = CommonDirectoryPath.GetSolutionDirectory();

        _productImage = new ImageFromDockerfileBuilder()
            .WithName("productsimulationapi:test")
            .WithDockerfile("ProductSimulation.API/Dockerfile")
            .WithDockerfileDirectory(solutionDirectory, ".")
            .Build();

        _pricingImage = new ImageFromDockerfileBuilder()
            .WithName("pricingsimulationapi:test")
            .WithDockerfile("PricingSimulation.API/Dockerfile")
            .WithDockerfileDirectory(solutionDirectory, ".")
            .Build();

        _stockImage = new ImageFromDockerfileBuilder()
            .WithName("stocksimulationapi:test")
            .WithDockerfile("StockSimulation.API/Dockerfile")
            .WithDockerfileDirectory(solutionDirectory, ".")
            .Build();

        _rabbitMqContainer = new RabbitMqBuilder("rabbitmq:3-management")
            .WithPortBinding(5672, true)
            .WithPortBinding(15672, true)
            .WithEnvironment("RABBITMQ_DEFAULT_USER", "guest")
            .WithEnvironment("RABBITMQ_DEFAULT_PASS", "guest")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilExternalTcpPortIsAvailable(5672))
            .Build();

        _productContainer = new ContainerBuilder(_productImage)
            .WithPortBinding(8080, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilExternalTcpPortIsAvailable(8080))
            .DependsOn(_rabbitMqContainer)
            .Build();

        _pricingContainer = new ContainerBuilder(_pricingImage)
            .WithPortBinding(8080, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilExternalTcpPortIsAvailable(8080))
            .DependsOn(_rabbitMqContainer)
            .Build();

        _stockContainer = new ContainerBuilder(_stockImage)
            .WithPortBinding(8080, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilExternalTcpPortIsAvailable(8080))
            .DependsOn(_rabbitMqContainer)
            .Build();
    }

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        await _productImage.CreateAsync();
        await _pricingImage.CreateAsync();
        await _stockImage.CreateAsync();

        await _rabbitMqContainer.StartAsync();
        await _productContainer.StartAsync();
        await _pricingContainer.StartAsync();
        await _stockContainer.StartAsync();

        Factory = new CustomWebApplicationFactory(
            ProductServiceUrl,
            PricingServiceUrl,
            StockServiceUrl,
            RabbitMqHost,
            RabbitMqPort);

        Client = Factory.CreateClient();
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        Factory.Dispose();

        await _productContainer.DisposeAsync();
        await _pricingContainer.DisposeAsync();
        await _stockContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
    }
}
