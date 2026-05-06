using Microsoft.EntityFrameworkCore;
using WebInventory.Application.Constants;
using WebInventory.Application.Interfaces;
using WebInventory.Domain.Entities;
using WebInventory.Infrastructure.Data;

namespace WebInventory.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private readonly ApplicationDbContext _dbContext;

    public InventoryService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Inventory>> GetAllAsync()
    {
        return await _dbContext.Inventories
            .AsNoTracking()
            .Include(i => i.Category)
            .OrderByDescending(i => i.UpdatedAt)
            .ToListAsync();
    }

    public async Task<Inventory?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Inventories
            .AsNoTracking()
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Inventory?> GetByIdForEditAsync(Guid id)
    {
        return await _dbContext.Inventories
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync()
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<string?> GetLatestCustomIdPatternAsync(Guid inventoryId)
    {
        return await _dbContext.CustomIdPatterns
            .AsNoTracking()
            .Where(p => p.InventoryId == inventoryId)
            .OrderByDescending(p => p.Version)
            .Select(p => p.Pattern)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> AddCustomIdPatternAsync(Guid inventoryId, string pattern)
    {
        var latest = await _dbContext.CustomIdPatterns
            .Where(p => p.InventoryId == inventoryId)
            .OrderByDescending(p => p.Version)
            .FirstOrDefaultAsync();

        if (latest is not null && string.Equals(latest.Pattern, pattern, StringComparison.Ordinal))
        {
            return false;
        }

        var nextVersion = latest?.Version + 1 ?? 1;
        _dbContext.CustomIdPatterns.Add(new CustomIdPattern
        {
            Id = Guid.NewGuid(),
            InventoryId = inventoryId,
            Version = nextVersion,
            Pattern = pattern,
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<Inventory> AddAsync(Inventory inventory)
    {
        _dbContext.Inventories.Add(inventory);
        if (!inventory.CustomIdPatterns.Any())
        {
            inventory.CustomIdPatterns.Add(new CustomIdPattern
            {
                Id = Guid.NewGuid(),
                InventoryId = inventory.Id,
                Version = 1,
                Pattern = CustomIdDefaults.DefaultPattern,
                CreatedAt = DateTime.UtcNow
            });
        }
        await _dbContext.SaveChangesAsync();
        return inventory;
    }

    public async Task<bool> UpdateAsync(Inventory inventory, uint rowVersion)
    {
        inventory.UpdatedAt = DateTime.UtcNow;
        _dbContext.Entry(inventory).Property(i => i.RowVersion).OriginalValue = rowVersion;
        try
        {
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    public async Task<bool> DeleteAsync(Inventory inventory)
    {
        _dbContext.Inventories.Remove(inventory);
        await _dbContext.SaveChangesAsync();
        return true;
    }
}
