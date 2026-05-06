using WebInventory.Domain.Enums;

namespace WebInventory.Domain.Entities;

public class InventoryAccess
{
    public Guid InventoryId { get; set; }
    public required string UserId { get; set; }
    public AccessLevel AccessLevel { get; set; }
    public Inventory? Inventory { get; set; }
}
