using System.Text;
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
        var pattern = await _dbContext.CustomIdPatterns
            .AsNoTracking()
            .Where(p => p.InventoryId == inventory.Id)
            .OrderByDescending(p => p.Version)
            .Select(p => p.Pattern)
            .FirstOrDefaultAsync();

        var resolvedPattern = string.IsNullOrWhiteSpace(pattern)
            ? CustomIdDefaults.DefaultPattern
            : pattern;

        var parts = _parser.Parse(resolvedPattern);
        var sequenceNumber = await _dbContext.Items
            .AsNoTracking()
            .Where(i => i.InventoryId == inventory.Id)
            .CountAsync() + 1;

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
}
