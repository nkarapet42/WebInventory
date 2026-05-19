using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WebInventory.Domain.Entities;
using WebInventory.Domain.Identity;
using WebInventory.Infrastructure.Data;
using WebInventory.Web.Hubs;
using WebInventory.Web.Models;
using WebInventory.Web.Services;

namespace WebInventory.Web.Controllers;

[Authorize]
public class CommentsController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IHubContext<DiscussionHub> _hubContext;
    private readonly MarkdownService _markdownService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CommentsController(
        ApplicationDbContext dbContext,
        IHubContext<DiscussionHub> hubContext,
        MarkdownService markdownService,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
        _markdownService = markdownService;
        _userManager = userManager;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCommentViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.BodyMarkdown))
        {
            return BadRequest("Comment text is required.");
        }

        var inventoryExists = await _dbContext.Inventories
            .AsNoTracking()
            .AnyAsync(inventory => inventory.Id == model.InventoryId);
        if (!inventoryExists)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Forbid();
        }

        var bodyMarkdown = model.BodyMarkdown.Trim();
        if (bodyMarkdown.Length > 4000)
        {
            return BadRequest("Comment text is too long.");
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            InventoryId = model.InventoryId,
            UserId = userId,
            BodyMarkdown = bodyMarkdown,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Comments.Add(comment);
        await _dbContext.SaveChangesAsync();

        var userName = User.Identity?.Name ?? "User";
        var payload = new
        {
            id = comment.Id,
            userId,
            userName,
            bodyHtml = _markdownService.ToHtml(comment.BodyMarkdown),
            createdAt = comment.CreatedAt.ToString("u")
        };

        await _hubContext.Clients
            .Group(DiscussionHub.GetGroupName(model.InventoryId.ToString()))
            .SendAsync("ReceiveComment", payload);

        return Json(payload);
    }
}
