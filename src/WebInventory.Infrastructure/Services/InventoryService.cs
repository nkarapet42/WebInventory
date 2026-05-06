using Microsoft.EntityFrameworkCore;
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

    public async Task<Inventory> AddAsync(Inventory inventory)
    {
        _dbContext.Inventories.Add(inventory);
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
