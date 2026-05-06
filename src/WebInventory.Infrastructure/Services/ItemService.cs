using Microsoft.EntityFrameworkCore;
using WebInventory.Application.Interfaces;
using WebInventory.Domain.Entities;
using WebInventory.Infrastructure.Data;

namespace WebInventory.Infrastructure.Services;

public class ItemService : IItemService
{
    private readonly ApplicationDbContext _dbContext;

    public ItemService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Item>> GetByInventoryAsync(Guid inventoryId)
    {
        return await _dbContext.Items
            .AsNoTracking()
            .Where(i => i.InventoryId == inventoryId)
            .OrderByDescending(i => i.UpdatedAt)
            .ToListAsync();
    }

    public async Task<Item?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Items
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Item?> GetByIdForEditAsync(Guid id)
    {
        return await _dbContext.Items
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Item> AddAsync(Item item)
    {
        _dbContext.Items.Add(item);
        await _dbContext.SaveChangesAsync();
        return item;
    }

    public async Task<bool> UpdateAsync(Item item, uint rowVersion)
    {
        item.UpdatedAt = DateTime.UtcNow;
        _dbContext.Entry(item).Property(i => i.RowVersion).OriginalValue = rowVersion;
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

    public async Task<bool> DeleteAsync(Item item)
    {
        _dbContext.Items.Remove(item);
        await _dbContext.SaveChangesAsync();
        return true;
    }
}
