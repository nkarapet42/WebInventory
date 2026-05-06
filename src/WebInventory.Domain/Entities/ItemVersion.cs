namespace WebInventory.Domain.Entities;

public class ItemVersion
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public int VersionNumber { get; set; }
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
    public Item? Item { get; set; }
}
