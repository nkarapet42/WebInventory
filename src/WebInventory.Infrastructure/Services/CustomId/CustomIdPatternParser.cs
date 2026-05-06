using WebInventory.Application.Models;
using WebInventory.Domain.Enums;

namespace WebInventory.Infrastructure.Services.CustomId;

public class CustomIdPatternParser
{
    private static readonly Dictionary<string, CustomIdPartType> TypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["FIX"] = CustomIdPartType.FixedText,
        ["R20"] = CustomIdPartType.Random20Bit,
        ["R32"] = CustomIdPartType.Random32Bit,
        ["R6"] = CustomIdPartType.Random6Digit,
        ["R9"] = CustomIdPartType.Random9Digit,
        ["GUID"] = CustomIdPartType.Guid,
        ["DATE"] = CustomIdPartType.DateTime,
        ["SEQ"] = CustomIdPartType.Sequence
    };

    public IReadOnlyList<CustomIdPartDefinition> Parse(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return Array.Empty<CustomIdPartDefinition>();
        }

        var parts = pattern.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var result = new List<CustomIdPartDefinition>();

        foreach (var part in parts)
        {
            var segments = part.Split(':', 2, StringSplitOptions.TrimEntries);
            if (segments.Length == 0)
            {
                continue;
            }

            if (!TypeMap.TryGetValue(segments[0], out var type))
            {
                continue;
            }

            var value = segments.Length > 1 ? segments[1] : null;
            result.Add(new CustomIdPartDefinition(type, value));
        }

        return result;
    }
}
