using WebInventory.Domain.Entities;

namespace WebInventory.Application.Models;

public record SearchResults(IReadOnlyList<Inventory> Inventories, IReadOnlyList<Item> Items, IReadOnlyList<Tag> Tags);
