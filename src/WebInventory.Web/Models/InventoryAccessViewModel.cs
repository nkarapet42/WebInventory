using WebInventory.Domain.Entities;
using WebInventory.Domain.Identity;

namespace WebInventory.Web.Models;

public class InventoryAccessViewModel
{
    public required Inventory Inventory { get; set; }
    public IReadOnlyList<ApplicationUser> Writers { get; set; } = Array.Empty<ApplicationUser>();
    public string? UserIdentifier { get; set; }
    public string SortMode { get; set; } = "name";
    public string? ErrorMessage { get; set; }
}
