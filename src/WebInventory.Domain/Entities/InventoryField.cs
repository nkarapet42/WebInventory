using WebInventory.Domain.Enums;

namespace WebInventory.Domain.Entities;

public class InventoryField
{
    public Guid Id { get; set; }
    public Guid InventoryId { get; set; }
    public InventoryFieldType FieldType { get; set; }
    public int SlotNumber { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public bool ShowInTable { get; set; }
    public int DisplayOrder { get; set; }
    public Inventory? Inventory { get; set; }
}
