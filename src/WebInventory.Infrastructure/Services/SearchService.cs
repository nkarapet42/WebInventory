using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes; // Ensure consistent usage of directives
using WebInventory.Application.Interfaces;
using WebInventory.Application.Models;
using WebInventory.Domain.Entities;
using WebInventory.Infrastructure.Data;

namespace WebInventory.Infrastructure.Services;

public partial class SearchService : ISearchService
{
    private readonly ApplicationDbContext _dbContext;

    public SearchService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SearchResults> SearchAsync(string term, int limit = 20)
    {
        var normalized = NormalizeTerm(term);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return new SearchResults(Array.Empty<Inventory>(), Array.Empty<Item>(), Array.Empty<Tag>());
        }

        var inventories = await _dbContext.Inventories
            .AsNoTracking()
            .Where(i => EF.Property<NpgsqlTsVector>(i, "SearchVector").Matches(EF.Functions.ToTsQuery("simple", normalized)))
            .OrderByDescending(i => i.UpdatedAt)
            .Take(limit)
            .ToListAsync();

        var items = await _dbContext.Items
            .AsNoTracking()
            .Where(i => EF.Property<NpgsqlTsVector>(i, "SearchVector").Matches(EF.Functions.ToTsQuery("simple", normalized)))
            .OrderByDescending(i => i.UpdatedAt)
            .Take(limit)
            .ToListAsync();

        var tags = await _dbContext.Tags
            .AsNoTracking()
            .Where(t => EF.Property<NpgsqlTsVector>(t, "SearchVector").Matches(EF.Functions.ToTsQuery("simple", normalized)))
            .OrderBy(t => t.Name)
            .Take(limit)
            .ToListAsync();

        return new SearchResults(inventories, items, tags);
    }

    private static string NormalizeTerm(string term)
    {
        var tokens = TermTokens().Matches(term)
            .Select(match => match.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Replace("'", string.Empty))
            .ToArray();

        return tokens.Length == 0 ? string.Empty : string.Join(" & ", tokens);
    }

    [GeneratedRegex(@"[\p{L}\p{N}]+")]
    private static partial Regex TermTokens();
}
