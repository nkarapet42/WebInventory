namespace WebInventory.Domain.Entities;

public class ItemLike
{
    public Guid ItemId { get; set; }
    public required string UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public Item? Item { get; set; }
}
