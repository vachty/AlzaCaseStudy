var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var products = new Dictionary<string, object>
{
    ["1"] = new { Id = "1", Name = "iPhone 15", ImageUrl = "https://example.com/images/iphone15.jpg" },
    ["2"] = new { Id = "2", Name = "Samsung Galaxy S24", ImageUrl = "https://example.com/images/galaxy-s24.jpg" },
    ["3"] = new { Id = "3", Name = "MacBook Air M3", ImageUrl = "https://example.com/images/macbook-air-m3.jpg" }
};

app.MapGet("/api/products/{productId}", (string productId) =>
{
    return products.TryGetValue(productId, out var product)
        ? Results.Ok(product)
        : Results.NotFound(new { message = $"Product '{productId}' not found." });
});

app.Run();
