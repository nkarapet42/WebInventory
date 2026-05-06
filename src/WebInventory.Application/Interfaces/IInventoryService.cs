using WebInventory.Domain.Entities;

namespace WebInventory.Application.Interfaces;

public interface IInventoryService
{
    Task<IReadOnlyList<Inventory>> GetAllAsync();
    Task<Inventory?> GetByIdAsync(Guid id);
    Task<Inventory?> GetByIdForEditAsync(Guid id);
    Task<IReadOnlyList<Category>> GetCategoriesAsync();
    Task<string?> GetLatestCustomIdPatternAsync(Guid inventoryId);
    Task<bool> AddCustomIdPatternAsync(Guid inventoryId, string pattern);
    Task<Inventory> AddAsync(Inventory inventory);
    Task<bool> UpdateAsync(Inventory inventory, uint rowVersion);
    Task<bool> DeleteAsync(Inventory inventory);
}
