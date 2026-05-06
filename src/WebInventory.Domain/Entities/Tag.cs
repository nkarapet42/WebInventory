namespace WebInventory.Domain.Entities;

public class Tag
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string NormalizedName { get; set; }
    public ICollection<InventoryTag> InventoryTags { get; set; } = new List<InventoryTag>();
    public ICollection<ItemTag> ItemTags { get; set; } = new List<ItemTag>();
}
