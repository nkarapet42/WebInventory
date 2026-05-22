using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using WebInventory.Application.Constants;
using WebInventory.Application.Interfaces;
using WebInventory.Application.Models;
using WebInventory.Domain.Entities;
using WebInventory.Domain.Enums;
using WebInventory.Infrastructure.Data;

namespace WebInventory.Infrastructure.Services.CustomId;

public class CustomIdGenerator : ICustomIdGenerator
{
    private readonly ApplicationDbContext _dbContext;
    private readonly CustomIdPatternParser _parser;
    private readonly IReadOnlyDictionary<CustomIdPartType, IIdPartGenerator> _generators;

    public CustomIdGenerator(ApplicationDbContext dbContext, IEnumerable<IIdPartGenerator> generators)
    {
        _dbContext = dbContext;
        _parser = new CustomIdPatternParser();
        _generators = generators.ToDictionary(g => g.PartType, g => g);
    }

    public async Task<string> GenerateAsync(Inventory inventory)
    {
        var resolvedPattern = await GetCurrentPatternAsync(inventory.Id);
        var parts = _parser.Parse(resolvedPattern);
        var sequenceNumber = await GetNextSequenceNumberAsync(inventory.Id, parts);

        var context = new CustomIdGenerationContext(inventory.Id, sequenceNumber);
        var builder = new StringBuilder();

        foreach (var part in parts)
        {
            if (_generators.TryGetValue(part.Type, out var generator))
            {
                builder.Append(generator.Generate(part, context));
            }
        }

        return builder.ToString();
    }

    public async Task<bool> MatchesCurrentPatternAsync(Inventory inventory, string customId)
    {
        if (string.IsNullOrWhiteSpace(customId))
        {
            return false;
        }

        var pattern = await GetCurrentPatternAsync(inventory.Id);
        var parts = _parser.Parse(pattern);
        if (parts.Count == 0)
        {
            return false;
        }

        return BuildRegex(parts, captureSequence: false).IsMatch(customId);
    }

    private async Task<string> GetCurrentPatternAsync(Guid inventoryId)
    {
        var pattern = await _dbContext.CustomIdPatterns
            .AsNoTracking()
            .Where(p => p.InventoryId == inventoryId)
            .OrderByDescending(p => p.Version)
            .Select(p => p.Pattern)
            .FirstOrDefaultAsync();

        return string.IsNullOrWhiteSpace(pattern)
            ? CustomIdDefaults.DefaultPattern
            : pattern;
    }

    private async Task<int> GetNextSequenceNumberAsync(Guid inventoryId, IReadOnlyList<CustomIdPartDefinition> parts)
    {
        if (!parts.Any(part => part.Type == CustomIdPartType.Sequence))
        {
            return 1;
        }

        var regex = BuildRegex(parts, captureSequence: true);
        var customIds = await _dbContext.Items
            .AsNoTracking()
            .Where(item => item.InventoryId == inventoryId)
            .Select(item => item.CustomId)
            .ToListAsync();

        var maxSequence = 0;
        foreach (var customId in customIds)
        {
            var match = regex.Match(customId);
            if (!match.Success)
            {
                continue;
            }

            foreach (Capture capture in match.Groups["seq"].Captures)
            {
                if (int.TryParse(capture.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
                {
                    maxSequence = Math.Max(maxSequence, value);
                }
            }
        }

        return maxSequence + 1;
    }

    private static Regex BuildRegex(IReadOnlyList<CustomIdPartDefinition> parts, bool captureSequence)
    {
        var builder = new StringBuilder("^");
        foreach (var part in parts)
        {
            builder.Append(part.Type switch
            {
                CustomIdPartType.FixedText => Regex.Escape(part.Value ?? string.Empty),
                CustomIdPartType.Random20Bit => "[0-9A-Fa-f]{5}",
                CustomIdPartType.Random32Bit => "[0-9A-Fa-f]{8}",
                CustomIdPartType.Random6Digit => "\\d{6}",
                CustomIdPartType.Random9Digit => "\\d{9}",
                CustomIdPartType.Guid => "[0-9A-Fa-f]{32}",
                CustomIdPartType.DateTime => BuildDateTimeRegex(part.Value),
                CustomIdPartType.Sequence => BuildSequenceRegex(part.Value, captureSequence),
                _ => throw new ArgumentOutOfRangeException(nameof(parts), part.Type, "Unsupported custom ID part type.")
            });
        }

        builder.Append('$');
        return new Regex(builder.ToString(), RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }

    private static string BuildSequenceRegex(string? value, bool captureSequence)
    {
        var minimumLength = int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) && parsed > 0
            ? parsed
            : 1;
        var pattern = $"\\d{{{minimumLength},}}";
        return captureSequence ? $"(?<seq>{pattern})" : pattern;
    }

    private static string BuildDateTimeRegex(string? value)
    {
        var format = string.IsNullOrWhiteSpace(value) ? "yyyyMMdd" : value;
        var builder = new StringBuilder();

        foreach (var character in format)
        {
            builder.Append(character switch
            {
                'y' or 'M' or 'd' or 'H' or 'h' or 'm' or 's' or 'f' or 'F' => "\\d",
                _ => Regex.Escape(character.ToString())
            });
        }

        return builder.ToString();
    }
}
