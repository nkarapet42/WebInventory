using System.Text.Json.Serialization;

namespace WebInventory.Web.Services;

public class SupportTicketDocument
{
    [JsonPropertyName("Summary")]
    public required string Summary { get; init; }

    [JsonPropertyName("Reported by")]
    public required string ReportedBy { get; init; }

    [JsonPropertyName("Inventory")]
    public string? Inventory { get; init; }

    [JsonPropertyName("Link")]
    public required string Link { get; init; }

    [JsonPropertyName("Priority")]
    public required string Priority { get; init; }

    [JsonPropertyName("Admins' e-mail addresses")]
    public required IReadOnlyList<string> AdminEmailAddresses { get; init; }

    [JsonPropertyName("Created at")]
    public required DateTime CreatedAt { get; init; }
}
