using WebInventory.Domain.Entities;

namespace WebInventory.Web.Models;

public class ItemFieldDisplayViewModel
{
    public required InventoryField Field { get; set; }
    public required string PropertyName { get; set; }
    public object? Value { get; set; }
}
