using System.ComponentModel.DataAnnotations;

namespace WebInventory.Web.Models;

public class SupportTicketViewModel
{
    [Required]
    [StringLength(500)]
    public string Summary { get; set; } = string.Empty;

    [Required]
    public string Priority { get; set; } = SupportTicketPriorities.Average;

    [Required]
    public string SourceUrl { get; set; } = string.Empty;

    public string? InventoryTitle { get; set; }
}

public static class SupportTicketPriorities
{
    public const string High = "High";
    public const string Average = "Average";
    public const string Low = "Low";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(
        [High, Average, Low],
        StringComparer.Ordinal);
}
