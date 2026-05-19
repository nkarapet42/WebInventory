using WebInventory.Domain.Entities;

namespace WebInventory.Web.Models;

public class InventoryDetailsViewModel
{
    public required Inventory Inventory { get; set; }
    public bool CanManage { get; set; }
    public bool CanWrite { get; set; }
    public int ItemCount { get; set; }
    public int CommentCount { get; set; }
    public string? LatestCustomIdPattern { get; set; }
    public IReadOnlyList<InventoryField> Fields { get; set; } = Array.Empty<InventoryField>();
    public IReadOnlyList<NumericFieldStatsViewModel> NumericStats { get; set; } = Array.Empty<NumericFieldStatsViewModel>();
}

public class NumericFieldStatsViewModel
{
    public required string Label { get; set; }
    public int FilledCount { get; set; }
    public decimal? Average { get; set; }
    public decimal? Minimum { get; set; }
    public decimal? Maximum { get; set; }
}
