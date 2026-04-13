using AggregationService;
using AggregationService.API;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.AddSerilogLogging();
builder.Services.AddTelemetry();

builder.Services.AddOptions();
builder.Services.AddServicesOptions(builder.Configuration);

builder.Services.AddClients();
builder.Services.AddServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseSerilogRequestLogging();
app.UseCorrelationId();

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
