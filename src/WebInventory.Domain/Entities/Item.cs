namespace WebInventory.Domain.Entities;

using WebInventory.Domain.Identity;

public class Item
{
    public Guid Id { get; set; }
    public Guid InventoryId { get; set; }
    public string? CreatedByUserId { get; set; }
    public required string CustomId { get; set; }
    public string? Text1 { get; set; }
    public string? Text2 { get; set; }
    public string? Text3 { get; set; }
    public string? Multiline1 { get; set; }
    public string? Multiline2 { get; set; }
    public string? Multiline3 { get; set; }
    public decimal? Num1 { get; set; }
    public decimal? Num2 { get; set; }
    public decimal? Num3 { get; set; }
    public string? Doc1 { get; set; }
    public string? Doc2 { get; set; }
    public string? Doc3 { get; set; }
    public bool? Bool1 { get; set; }
    public bool? Bool2 { get; set; }
    public bool? Bool3 { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public uint RowVersion { get; set; }
    public Inventory? Inventory { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public ICollection<ItemTag> ItemTags { get; set; } = new List<ItemTag>();
    public ICollection<ItemLike> Likes { get; set; } = new List<ItemLike>();
    public ICollection<ItemVersion> Versions { get; set; } = new List<ItemVersion>();
}
