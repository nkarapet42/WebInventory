using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using WebInventory.Domain.Identity;
using WebInventory.Infrastructure.Data;
using WebInventory.Web.Models;
using WebInventory.Web.Services;

namespace WebInventory.Web.Controllers;

[Authorize]
public class SupportTicketsController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISupportTicketUploader _uploader;
    private readonly ILogger<SupportTicketsController> _logger;

    public SupportTicketsController(
        ApplicationDbContext dbContext,
        IConfiguration configuration,
        UserManager<ApplicationUser> userManager,
        ISupportTicketUploader uploader,
        ILogger<SupportTicketsController> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _userManager = userManager;
        _uploader = uploader;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Create(string? sourceUrl)
    {
        var localSourceUrl = NormalizeLocalUrl(sourceUrl);
        return View(new SupportTicketViewModel
        {
            SourceUrl = localSourceUrl,
            InventoryTitle = await ResolveInventoryTitleAsync(localSourceUrl)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupportTicketViewModel model, CancellationToken cancellationToken)
    {
        model.SourceUrl = NormalizeLocalUrl(model.SourceUrl);
        model.InventoryTitle = await ResolveInventoryTitleAsync(model.SourceUrl);

        if (!SupportTicketPriorities.All.Contains(model.Priority))
        {
            ModelState.AddModelError(nameof(model.Priority), "Choose a valid priority.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var ticket = new SupportTicketDocument
        {
            Summary = model.Summary.Trim(),
            ReportedBy = user.Email ?? user.UserName ?? user.Id,
            Inventory = model.InventoryTitle,
            Link = BuildAbsoluteUrl(model.SourceUrl),
            Priority = model.Priority,
            AdminEmailAddresses = GetAdminEmailAddresses(),
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await _uploader.UploadAsync(ticket, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not upload a support ticket for user {UserId}.", user.Id);
            ModelState.AddModelError(string.Empty, "Could not upload the support ticket. Check the Dropbox integration configuration and try again.");
            return View(model);
        }

        TempData["SupportTicketSuccess"] = "Support ticket uploaded. Administrators will be notified.";
        return Redirect(model.SourceUrl);
    }

    private string BuildAbsoluteUrl(string localUrl)
    {
        return $"{Request.Scheme}://{Request.Host}{Request.PathBase}{localUrl}";
    }

    private string[] GetAdminEmailAddresses()
    {
        return _configuration.GetSection("Admin:Emails").Get<string[]>()
            ?? SplitEmails(_configuration["ADMIN_EMAILS"]);
    }

    private async Task<string?> ResolveInventoryTitleAsync(string localUrl)
    {
        var uri = new Uri($"http://local{localUrl}");
        var query = QueryHelpers.ParseQuery(uri.Query);
        Guid? inventoryId = query.TryGetValue("inventoryId", out var inventoryIdValue)
            ? TryParseGuid(inventoryIdValue)
            : null;
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (inventoryId is null && segments.Length >= 3 && segments[0].Equals("Inventories", StringComparison.OrdinalIgnoreCase))
        {
            inventoryId = TryParseGuid(segments[2]);
        }

        if (inventoryId is null
            && segments.Length >= 3
            && segments[0].Equals("Items", StringComparison.OrdinalIgnoreCase)
            && TryParseGuid(segments[2]) is { } itemId)
        {
            inventoryId = await _dbContext.Items
                .AsNoTracking()
                .Where(item => item.Id == itemId)
                .Select(item => (Guid?)item.InventoryId)
                .FirstOrDefaultAsync();
        }

        return inventoryId is null
            ? null
            : await _dbContext.Inventories
                .AsNoTracking()
                .Where(inventory => inventory.Id == inventoryId)
                .Select(inventory => inventory.Title)
                .FirstOrDefaultAsync();
    }

    private static string NormalizeLocalUrl(string? sourceUrl)
    {
        return string.IsNullOrWhiteSpace(sourceUrl)
            || !Uri.TryCreate(sourceUrl, UriKind.Relative, out _)
            || !sourceUrl.StartsWith('/')
            || sourceUrl.StartsWith("//", StringComparison.Ordinal)
                ? "/"
                : sourceUrl;
    }

    private static Guid? TryParseGuid(string? value)
    {
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private static string[] SplitEmails(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Array.Empty<string>()
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
