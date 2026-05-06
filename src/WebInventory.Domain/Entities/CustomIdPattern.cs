namespace WebInventory.Domain.Entities;

public class CustomIdPattern
{
    public Guid Id { get; set; }
    public Guid InventoryId { get; set; }
    public int Version { get; set; }
    public required string Pattern { get; set; }
    public DateTime CreatedAt { get; set; }
    public Inventory? Inventory { get; set; }
}
