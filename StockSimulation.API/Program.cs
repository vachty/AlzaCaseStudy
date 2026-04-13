var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var stock = new Dictionary<string, object>
{
    ["1"] = new { ProductId = "1", Quantity = 12, InStock = true },
    ["2"] = new { ProductId = "2", Quantity = 0, InStock = false },
    ["3"] = new { ProductId = "3", Quantity = 4, InStock = true }
};

app.MapGet("/api/stock/{productId}", (string productId) =>
{
    return stock.TryGetValue(productId, out var item)
        ? Results.Ok(item)
        : Results.NotFound(new { message = $"Stock for product '{productId}' not found." });
});

app.Run();