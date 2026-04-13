using AggregationService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace AggregationService.Controllers;

[ApiController]
[Route("api/v1/products")]
public class ProductsController : ControllerBase
{
    private readonly ProductAggregationService _aggregationService;

    public ProductsController(ProductAggregationService aggregationService)
    {
        _aggregationService = aggregationService;
    }

    [HttpGet("{productId}")]
    public async Task<IActionResult> GetById(string productId, CancellationToken cancellationToken)
    {
        var result = await _aggregationService.GetByIdAsync(productId, cancellationToken);
        if (result is null)
        {
            return NotFound(new { message = $"Product '{productId}' was not found." });
        }

        return Ok(result);
    }
}
