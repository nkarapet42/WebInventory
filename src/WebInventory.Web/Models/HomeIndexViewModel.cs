namespace WebInventory.Web.Models;

public class HomeIndexViewModel
{
    public IReadOnlyList<HomeInventoryViewModel> LatestInventories { get; set; } = Array.Empty<HomeInventoryViewModel>();
    public IReadOnlyList<HomeInventoryViewModel> PopularInventories { get; set; } = Array.Empty<HomeInventoryViewModel>();
    public IReadOnlyList<TagCloudItemViewModel> TagCloud { get; set; } = Array.Empty<TagCloudItemViewModel>();
}

public class HomeInventoryViewModel
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? ImageUrl { get; set; }
    public string? CategoryName { get; set; }
    public string? CreatorName { get; set; }
    public int ItemCount { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TagCloudItemViewModel
{
    public required string Name { get; set; }
    public int InventoryCount { get; set; }
    public int Weight { get; set; }
}
