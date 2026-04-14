using AggregationService.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace AggregationService.API.Controllers;

[ApiController]
[Route("api/v1/products")]
public class ProductsController(IProductAggregationService aggregationService) : ControllerBase
{
    [HttpGet("{productId}")]
    public async Task<IActionResult> GetById(string productId, CancellationToken cancellationToken)
    {
        var result = await aggregationService.GetByIdAsync(productId, cancellationToken);
        if (result is null)
        {
            return NotFound(new { message = $"Product '{productId}' was not found." });
        }

        return Ok(result);
    }
}
