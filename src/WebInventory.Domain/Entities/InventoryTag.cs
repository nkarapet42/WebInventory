namespace WebInventory.Domain.Entities;

public class InventoryTag
{
    public Guid InventoryId { get; set; }
    public Guid TagId { get; set; }
    public Inventory? Inventory { get; set; }
    public Tag? Tag { get; set; }
}
