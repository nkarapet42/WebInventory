using WebInventory.Domain.Enums;

namespace WebInventory.Domain.Entities;

public class Inventory
{
    public Guid Id { get; set; }
    public required string OwnerId { get; set; }
    public Guid? CategoryId { get; set; }
    public required string Title { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? ImageUrl { get; set; }
    public string? ApiTokenHash { get; set; }
    public DateTime? ApiTokenCreatedAt { get; set; }
    public InventoryAccessMode AccessMode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public uint RowVersion { get; set; }
    public Category? Category { get; set; }
    public ICollection<InventoryField> Fields { get; set; } = new List<InventoryField>();
    public ICollection<Item> Items { get; set; } = new List<Item>();
    public ICollection<InventoryAccess> AccessList { get; set; } = new List<InventoryAccess>();
    public ICollection<InventoryTag> InventoryTags { get; set; } = new List<InventoryTag>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<CustomIdPattern> CustomIdPatterns { get; set; } = new List<CustomIdPattern>();
}
