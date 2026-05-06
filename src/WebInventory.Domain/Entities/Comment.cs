namespace WebInventory.Domain.Entities;

public class Comment
{
    public Guid Id { get; set; }
    public Guid InventoryId { get; set; }
    public required string UserId { get; set; }
    public required string BodyMarkdown { get; set; }
    public DateTime CreatedAt { get; set; }
    public Inventory? Inventory { get; set; }
}
