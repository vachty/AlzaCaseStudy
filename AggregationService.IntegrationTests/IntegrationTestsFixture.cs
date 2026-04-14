using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using Testcontainers.RabbitMq;
using Xunit;

namespace AggregationService.IntegrationTests;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    private readonly IFutureDockerImage productImage;
    private readonly IFutureDockerImage pricingImage;
    private readonly IFutureDockerImage stockImage;

    private readonly IContainer productContainer;
    private readonly IContainer pricingContainer;
    private readonly IContainer stockContainer;
    private readonly IContainer rabbitMqContainer;

    public CustomWebApplicationFactory Factory { get; private set; } = default!;
    public HttpClient Client { get; private set; } = default!;

    public string ProductServiceUrl => $"http://localhost:{productContainer.GetMappedPublicPort(8080)}";
    public string PricingServiceUrl => $"http://localhost:{pricingContainer.GetMappedPublicPort(8080)}";
    public string StockServiceUrl => $"http://localhost:{stockContainer.GetMappedPublicPort(8080)}";

    public string RabbitMqHost => "localhost";
    public ushort RabbitMqPort => (ushort)rabbitMqContainer.GetMappedPublicPort(5672);

    /// <summary>
    /// .ctor
    /// </summary>
    public IntegrationTestFixture()
    {
        var solutionDirectory = CommonDirectoryPath.GetSolutionDirectory();

        productImage = new ImageFromDockerfileBuilder()
            .WithName("productsimulationapi:test")
            .WithDockerfile("ProductSimulation.API/Dockerfile")
            .WithDockerfileDirectory(solutionDirectory, ".")
            .Build();

        pricingImage = new ImageFromDockerfileBuilder()
            .WithName("pricingsimulationapi:test")
            .WithDockerfile("PricingSimulation.API/Dockerfile")
            .WithDockerfileDirectory(solutionDirectory, ".")
            .Build();

        stockImage = new ImageFromDockerfileBuilder()
            .WithName("stocksimulationapi:test")
            .WithDockerfile("StockSimulation.API/Dockerfile")
            .WithDockerfileDirectory(solutionDirectory, ".")
            .Build();

        rabbitMqContainer = new RabbitMqBuilder("rabbitmq:3-management")
            .WithPortBinding(5672, true)
            .WithPortBinding(15672, true)
            .WithEnvironment("RABBITMQ_DEFAULT_USER", "guest")
            .WithEnvironment("RABBITMQ_DEFAULT_PASS", "guest")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilExternalTcpPortIsAvailable(5672))
            .Build();

        productContainer = new ContainerBuilder(productImage)
            .WithPortBinding(8080, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilExternalTcpPortIsAvailable(8080))
            .DependsOn(rabbitMqContainer)
            .Build();

        pricingContainer = new ContainerBuilder(pricingImage)
            .WithPortBinding(8080, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilExternalTcpPortIsAvailable(8080))
            .DependsOn(rabbitMqContainer)
            .Build();

        stockContainer = new ContainerBuilder(stockImage)
            .WithPortBinding(8080, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilExternalTcpPortIsAvailable(8080))
            .DependsOn(rabbitMqContainer)
            .Build();
    }

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        await productImage.CreateAsync();
        await pricingImage.CreateAsync();
        await stockImage.CreateAsync();

        await rabbitMqContainer.StartAsync();
        await productContainer.StartAsync();
        await pricingContainer.StartAsync();
        await stockContainer.StartAsync();

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

        await productContainer.DisposeAsync();
        await pricingContainer.DisposeAsync();
        await stockContainer.DisposeAsync();
        await rabbitMqContainer.DisposeAsync();
    }
}
