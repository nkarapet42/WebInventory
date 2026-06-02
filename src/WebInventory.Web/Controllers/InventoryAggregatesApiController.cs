using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebInventory.Application.Interfaces;
using WebInventory.Infrastructure.Data;
using WebInventory.Web.Services;

namespace WebInventory.Web.Controllers;

[ApiController]
[Route("api/inventories/aggregates")]
public class InventoryAggregatesApiController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IInventoryAggregationService _aggregationService;

    public InventoryAggregatesApiController(
        ApplicationDbContext dbContext,
        IInventoryAggregationService aggregationService)
    {
        _dbContext = dbContext;
        _aggregationService = aggregationService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized();
        }

        var token = authorization["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Unauthorized();
        }

        var tokenHash = InventoryApiTokenService.Hash(token);
        var inventoryId = await _dbContext.Inventories
            .AsNoTracking()
            .Where(inventory => inventory.ApiTokenHash == tokenHash)
            .Select(inventory => (Guid?)inventory.Id)
            .FirstOrDefaultAsync();
        if (inventoryId is null)
        {
            return Unauthorized();
        }

        var result = await _aggregationService.GetAsync(inventoryId.Value);
        return result is null ? NotFound() : Ok(result);
    }
}
