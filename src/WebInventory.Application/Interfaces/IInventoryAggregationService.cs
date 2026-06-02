using WebInventory.Application.Models;

namespace WebInventory.Application.Interfaces;

public interface IInventoryAggregationService
{
    Task<InventoryAggregateResult?> GetAsync(Guid inventoryId);
}
