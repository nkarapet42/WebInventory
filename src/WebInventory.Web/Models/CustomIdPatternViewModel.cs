using System.ComponentModel.DataAnnotations;

namespace WebInventory.Web.Models;

public class CustomIdPatternViewModel
{
    public Guid InventoryId { get; set; }

    [StringLength(1000)]
    public string? Pattern { get; set; }

    public string? Preview { get; set; }
}
