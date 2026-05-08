using WebInventory.Domain.Entities;

namespace WebInventory.Web.Models;

public class SearchViewModel
{
    public string Query { get; set; } = string.Empty;
    public IReadOnlyList<Inventory> Inventories { get; set; } = Array.Empty<Inventory>();
    public IReadOnlyList<Item> Items { get; set; } = Array.Empty<Item>();
    public IReadOnlyList<Tag> Tags { get; set; } = Array.Empty<Tag>();
}
