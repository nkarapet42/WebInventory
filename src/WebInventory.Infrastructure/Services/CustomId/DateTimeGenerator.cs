using WebInventory.Application.Interfaces;
using WebInventory.Application.Models;
using WebInventory.Domain.Enums;

namespace WebInventory.Infrastructure.Services.CustomId;

public class DateTimeGenerator : IIdPartGenerator
{
    public CustomIdPartType PartType => CustomIdPartType.DateTime;

    public string Generate(CustomIdPartDefinition part, CustomIdGenerationContext context)
    {
        var format = string.IsNullOrWhiteSpace(part.Value) ? "yyyyMMdd" : part.Value;
        return DateTime.UtcNow.ToString(format);
    }
}
