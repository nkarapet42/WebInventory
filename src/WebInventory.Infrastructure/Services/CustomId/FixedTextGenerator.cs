using WebInventory.Application.Interfaces;
using WebInventory.Application.Models;
using WebInventory.Domain.Enums;

namespace WebInventory.Infrastructure.Services.CustomId;

public class FixedTextGenerator : IIdPartGenerator
{
    public CustomIdPartType PartType => CustomIdPartType.FixedText;

    public string Generate(CustomIdPartDefinition part, CustomIdGenerationContext context)
    {
        return part.Value ?? string.Empty;
    }
}
