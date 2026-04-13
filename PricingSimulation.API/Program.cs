var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var prices = new Dictionary<string, object>
{
    ["1"] = new { ProductId = "1", Amount = 25990m, Currency = "CZK" },
    ["2"] = new { ProductId = "2", Amount = 21990m, Currency = "CZK" },
    ["3"] = new { ProductId = "3", Amount = 32990m, Currency = "CZK" }
};

app.MapGet("/api/prices/{productId}", async (string productId, CancellationToken ct) =>
{
    await Task.Delay(Random.Shared.Next(500, 801), ct);

    if (Random.Shared.Next(1, 101) <= 20)
    {
        return Results.Problem(
            title: "Pricing service temporary failure",
            detail: "Simulated downstream failure.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return prices.TryGetValue(productId, out var price)
        ? Results.Ok(price)
        : Results.NotFound(new { message = $"Price for product '{productId}' not found." });
});

app.Run();