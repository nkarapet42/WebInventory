using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebInventory.Domain.Constants;
using WebInventory.Domain.Enums;
using WebInventory.Domain.Identity;
using WebInventory.Infrastructure.Data;
using WebInventory.Web.Models;

namespace WebInventory.Web.Controllers;

public class UsersController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [Authorize]
    public IActionResult Me()
    {
        var userId = _userManager.GetUserId(User);
        return string.IsNullOrWhiteSpace(userId)
            ? RedirectToAction("Index", "Home")
            : RedirectToAction(nameof(Profile), new { id = userId });
    }

    [AllowAnonymous]
    public async Task<IActionResult> Profile(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return RedirectToAction("Index", "Home");
        }

        var user = await _userManager.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.Email,
                u.SalesforceAccountId,
                u.SalesforceContactId,
                u.SalesforceSyncedAt
            })
            .FirstOrDefaultAsync();

        if (user is null)
        {
            return View("NotFound", id);
        }

        var currentUserId = _userManager.GetUserId(User);
        var model = new UserProfileViewModel
        {
            UserId = user.Id,
            DisplayName = user.UserName ?? user.Email ?? "User",
            Email = user.Email,
            IsCurrentUser = string.Equals(currentUserId, user.Id, StringComparison.Ordinal),
            CanCreateSalesforceCustomer = string.IsNullOrWhiteSpace(user.SalesforceAccountId)
                && string.IsNullOrWhiteSpace(user.SalesforceContactId)
                && (string.Equals(currentUserId, user.Id, StringComparison.Ordinal)
                    || User.IsInRole(RoleNames.Admin)),
            SalesforceAccountId = user.SalesforceAccountId,
            SalesforceContactId = user.SalesforceContactId,
            SalesforceSyncedAt = user.SalesforceSyncedAt,
            OwnedInventories = await GetOwnedInventoriesAsync(user.Id),
            WritableInventories = await GetWritableInventoriesAsync(user.Id)
        };

        return View(model);
    }

    private async Task<IReadOnlyList<UserInventoryRowViewModel>> GetOwnedInventoriesAsync(string userId)
    {
        return await _dbContext.Inventories
            .AsNoTracking()
            .Where(inventory => inventory.OwnerId == userId)
            .OrderByDescending(inventory => inventory.UpdatedAt)
            .Select(inventory => new UserInventoryRowViewModel
            {
                Id = inventory.Id,
                Title = inventory.Title,
                CategoryName = inventory.Category == null ? null : inventory.Category.Name,
                OwnerName = inventory.OwnerId,
                ItemCount = inventory.Items.Count,
                AccessMode = inventory.AccessMode.ToString(),
                UpdatedAt = inventory.UpdatedAt
            })
            .ToListAsync();
    }

    private async Task<IReadOnlyList<UserInventoryRowViewModel>> GetWritableInventoriesAsync(string userId)
    {
        return await _dbContext.Inventories
            .AsNoTracking()
            .Where(inventory => inventory.OwnerId != userId &&
                (inventory.AccessMode == InventoryAccessMode.PublicWrite ||
                 inventory.AccessList.Any(access => access.UserId == userId && access.AccessLevel == AccessLevel.Write)))
            .OrderByDescending(inventory => inventory.UpdatedAt)
            .Select(inventory => new UserInventoryRowViewModel
            {
                Id = inventory.Id,
                Title = inventory.Title,
                CategoryName = inventory.Category == null ? null : inventory.Category.Name,
                OwnerName = _dbContext.Users
                    .Where(user => user.Id == inventory.OwnerId)
                    .Select(user => user.UserName ?? user.Email ?? "User")
                    .FirstOrDefault() ?? "User",
                ItemCount = inventory.Items.Count,
                AccessMode = inventory.AccessMode.ToString(),
                UpdatedAt = inventory.UpdatedAt
            })
            .ToListAsync();
    }
}
