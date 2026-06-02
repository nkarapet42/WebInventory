namespace WebInventory.Application.Models;

public class InventoryAggregateResult
{
    public Guid InventoryId { get; set; }
    public required string Title { get; set; }
    public int ItemCount { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime GeneratedAt { get; set; }
    public IReadOnlyList<InventoryFieldAggregateResult> Fields { get; set; } = [];
}

public class InventoryFieldAggregateResult
{
    public required string Title { get; set; }
    public required string Type { get; set; }
    public int FilledCount { get; set; }
    public decimal? Average { get; set; }
    public decimal? Minimum { get; set; }
    public decimal? Maximum { get; set; }
    public int? TrueCount { get; set; }
    public int? FalseCount { get; set; }
    public IReadOnlyList<InventoryValueFrequencyResult> TopValues { get; set; } = [];
}

public class InventoryValueFrequencyResult
{
    public required string Value { get; set; }
    public int Count { get; set; }
}
