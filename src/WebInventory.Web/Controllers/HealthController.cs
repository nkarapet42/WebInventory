using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebInventory.Infrastructure.Data;

namespace WebInventory.Web.Controllers;

[AllowAnonymous]
[ApiController]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public HealthController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("/healthz")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
        return canConnect
            ? Ok(new { status = "healthy" })
            : StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "unhealthy" });
    }
}
