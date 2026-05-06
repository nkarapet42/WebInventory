using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebInventory.Application.Interfaces;
using WebInventory.Application.Models;
using WebInventory.Domain.Entities;
using WebInventory.Domain.Enums;
using WebInventory.Domain.Identity;
using WebInventory.Infrastructure.Data;

namespace WebInventory.Infrastructure.Services;

public class AccessControlService : IAccessControlService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccessControlService(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<bool> CanWriteAsync(Inventory inventory, ClaimsPrincipal user)
    {
        if (inventory.AccessMode == InventoryAccessMode.PublicWrite)
        {
            return user.Identity?.IsAuthenticated == true;
        }

        var userId = _userManager.GetUserId(user);
        if (userId is null)
        {
            return false;
        }

        if (inventory.OwnerId == userId)
        {
            return true;
        }

        return await _dbContext.InventoryAccesses
            .AsNoTracking()
            .AnyAsync(a => a.InventoryId == inventory.Id && a.UserId == userId && a.AccessLevel == AccessLevel.Write);
    }

    public async Task<IReadOnlyList<ApplicationUser>> GetWritersAsync(Guid inventoryId)
    {
        return await _dbContext.InventoryAccesses
            .AsNoTracking()
            .Where(a => a.InventoryId == inventoryId && a.AccessLevel == AccessLevel.Write)
            .Join(_dbContext.Users, a => a.UserId, u => u.Id, (_, u) => u)
            .OrderBy(u => u.UserName)
            .ToListAsync();
    }

    public async Task<AccessChangeResult> AddWriterAsync(Guid inventoryId, string userIdentifier)
    {
        if (string.IsNullOrWhiteSpace(userIdentifier))
        {
            return AccessChangeResult.Failed("Enter a username or email.");
        }

        var user = await _userManager.FindByEmailAsync(userIdentifier);
        if (user is null)
        {
            user = await _userManager.FindByNameAsync(userIdentifier);
        }

        if (user is null)
        {
            return AccessChangeResult.Failed("User not found.");
        }

        var existing = await _dbContext.InventoryAccesses
            .AsNoTracking()
            .AnyAsync(a => a.InventoryId == inventoryId && a.UserId == user.Id);
        if (existing)
        {
            return AccessChangeResult.Failed("User already has access.");
        }

        _dbContext.InventoryAccesses.Add(new InventoryAccess
        {
            InventoryId = inventoryId,
            UserId = user.Id,
            AccessLevel = AccessLevel.Write
        });

        await _dbContext.SaveChangesAsync();
        return AccessChangeResult.Success();
    }

    public async Task<AccessChangeResult> RemoveWriterAsync(Guid inventoryId, string userId)
    {
        var access = await _dbContext.InventoryAccesses
            .FirstOrDefaultAsync(a => a.InventoryId == inventoryId && a.UserId == userId);

        if (access is null)
        {
            return AccessChangeResult.Failed("Access not found.");
        }

        if (access.AccessLevel == AccessLevel.Owner)
        {
            return AccessChangeResult.Failed("Owner access cannot be removed.");
        }

        _dbContext.InventoryAccesses.Remove(access);
        await _dbContext.SaveChangesAsync();
        return AccessChangeResult.Success();
    }
}
