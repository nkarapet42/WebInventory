using Microsoft.EntityFrameworkCore;
using WebInventory.Application.Interfaces;
using WebInventory.Application.Models;
using WebInventory.Domain.Entities;
using WebInventory.Domain.Enums;
using WebInventory.Infrastructure.Data;

namespace WebInventory.Infrastructure.Services;

public class InventoryAggregationService : IInventoryAggregationService
{
    private readonly ApplicationDbContext _dbContext;

    public InventoryAggregationService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<InventoryAggregateResult?> GetAsync(Guid inventoryId)
    {
        var inventory = await _dbContext.Inventories
            .AsNoTracking()
            .Where(row => row.Id == inventoryId)
            .Select(row => new
            {
                row.Id,
                row.Title,
                row.UpdatedAt,
                ItemCount = row.Items.Count
            })
            .FirstOrDefaultAsync();
        if (inventory is null)
        {
            return null;
        }

        var fields = await _dbContext.InventoryFields
            .AsNoTracking()
            .Where(field => field.InventoryId == inventoryId)
            .OrderBy(field => field.DisplayOrder)
            .ThenBy(field => field.FieldType)
            .ThenBy(field => field.SlotNumber)
            .ToListAsync();

        var results = new List<InventoryFieldAggregateResult>(fields.Count);
        foreach (var field in fields)
        {
            results.Add(await BuildFieldAsync(inventoryId, field));
        }

        return new InventoryAggregateResult
        {
            InventoryId = inventory.Id,
            Title = inventory.Title,
            ItemCount = inventory.ItemCount,
            UpdatedAt = inventory.UpdatedAt,
            GeneratedAt = DateTime.UtcNow,
            Fields = results
        };
    }

    private async Task<InventoryFieldAggregateResult> BuildFieldAsync(Guid inventoryId, InventoryField field)
    {
        return field.FieldType switch
        {
            InventoryFieldType.Number => await BuildNumberAsync(inventoryId, field),
            InventoryFieldType.Text => await BuildTextAsync(inventoryId, field),
            InventoryFieldType.Multiline => await BuildMultilineAsync(inventoryId, field),
            InventoryFieldType.Boolean => await BuildBooleanAsync(inventoryId, field),
            _ => new InventoryFieldAggregateResult
            {
                Title = field.Title,
                Type = field.FieldType.ToString()
            }
        };
    }

    private async Task<InventoryFieldAggregateResult> BuildNumberAsync(Guid inventoryId, InventoryField field)
    {
        var items = _dbContext.Items.AsNoTracking().Where(item => item.InventoryId == inventoryId);
        var values = field.SlotNumber switch
        {
            1 => items.Where(item => item.Num1.HasValue).Select(item => item.Num1!.Value),
            2 => items.Where(item => item.Num2.HasValue).Select(item => item.Num2!.Value),
            3 => items.Where(item => item.Num3.HasValue).Select(item => item.Num3!.Value),
            _ => throw InvalidSlot(field)
        };
        var count = await values.CountAsync();
        return new InventoryFieldAggregateResult
        {
            Title = field.Title,
            Type = field.FieldType.ToString(),
            FilledCount = count,
            Average = count == 0 ? null : await values.AverageAsync(),
            Minimum = count == 0 ? null : await values.MinAsync(),
            Maximum = count == 0 ? null : await values.MaxAsync()
        };
    }

    private async Task<InventoryFieldAggregateResult> BuildTextAsync(Guid inventoryId, InventoryField field)
    {
        var items = _dbContext.Items.AsNoTracking().Where(item => item.InventoryId == inventoryId);
        var values = field.SlotNumber switch
        {
            1 => items.Where(item => item.Text1 != null && item.Text1 != string.Empty).Select(item => item.Text1!),
            2 => items.Where(item => item.Text2 != null && item.Text2 != string.Empty).Select(item => item.Text2!),
            3 => items.Where(item => item.Text3 != null && item.Text3 != string.Empty).Select(item => item.Text3!),
            _ => throw InvalidSlot(field)
        };
        return await BuildStringResultAsync(field, values);
    }

    private async Task<InventoryFieldAggregateResult> BuildMultilineAsync(Guid inventoryId, InventoryField field)
    {
        var items = _dbContext.Items.AsNoTracking().Where(item => item.InventoryId == inventoryId);
        var values = field.SlotNumber switch
        {
            1 => items.Where(item => item.Multiline1 != null && item.Multiline1 != string.Empty).Select(item => item.Multiline1!),
            2 => items.Where(item => item.Multiline2 != null && item.Multiline2 != string.Empty).Select(item => item.Multiline2!),
            3 => items.Where(item => item.Multiline3 != null && item.Multiline3 != string.Empty).Select(item => item.Multiline3!),
            _ => throw InvalidSlot(field)
        };
        return await BuildStringResultAsync(field, values);
    }

    private static async Task<InventoryFieldAggregateResult> BuildStringResultAsync(
        InventoryField field,
        IQueryable<string> values)
    {
        return new InventoryFieldAggregateResult
        {
            Title = field.Title,
            Type = field.FieldType.ToString(),
            FilledCount = await values.CountAsync(),
            TopValues = await values
                .GroupBy(value => value)
                .Select(group => new InventoryValueFrequencyResult
                {
                    Value = group.Key,
                    Count = group.Count()
                })
                .OrderByDescending(row => row.Count)
                .ThenBy(row => row.Value)
                .Take(5)
                .ToListAsync()
        };
    }

    private async Task<InventoryFieldAggregateResult> BuildBooleanAsync(Guid inventoryId, InventoryField field)
    {
        var items = _dbContext.Items.AsNoTracking().Where(item => item.InventoryId == inventoryId);
        var values = field.SlotNumber switch
        {
            1 => items.Where(item => item.Bool1.HasValue).Select(item => item.Bool1!.Value),
            2 => items.Where(item => item.Bool2.HasValue).Select(item => item.Bool2!.Value),
            3 => items.Where(item => item.Bool3.HasValue).Select(item => item.Bool3!.Value),
            _ => throw InvalidSlot(field)
        };
        return new InventoryFieldAggregateResult
        {
            Title = field.Title,
            Type = field.FieldType.ToString(),
            FilledCount = await values.CountAsync(),
            TrueCount = await values.CountAsync(value => value),
            FalseCount = await values.CountAsync(value => !value)
        };
    }

    private static ArgumentOutOfRangeException InvalidSlot(InventoryField field)
    {
        return new ArgumentOutOfRangeException(
            nameof(field.SlotNumber),
            field.SlotNumber,
            $"Unsupported {field.FieldType} field slot.");
    }
}
