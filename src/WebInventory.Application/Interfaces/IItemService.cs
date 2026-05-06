using WebInventory.Domain.Entities;

namespace WebInventory.Application.Interfaces;

public interface IItemService
{
    Task<IReadOnlyList<Item>> GetByInventoryAsync(Guid inventoryId);
    Task<Item?> GetByIdAsync(Guid id);
    Task<Item?> GetByIdForEditAsync(Guid id);
    Task<Item> AddAsync(Item item);
    Task<bool> UpdateAsync(Item item, uint rowVersion);
    Task<bool> DeleteAsync(Item item);
}
