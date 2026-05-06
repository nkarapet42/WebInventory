using System.ComponentModel.DataAnnotations;

namespace WebInventory.Web.Models;

public class ItemFormViewModel
{
    public Guid? Id { get; set; }
    public Guid InventoryId { get; set; }

    [StringLength(120)]
    public string? CustomId { get; set; }

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

    public uint? RowVersion { get; set; }
}
