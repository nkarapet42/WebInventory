using WebInventory.Application.Interfaces;
using WebInventory.Application.Models;
using WebInventory.Domain.Enums;

namespace WebInventory.Infrastructure.Services.CustomId;

public class SequenceGenerator : IIdPartGenerator
{
    public CustomIdPartType PartType => CustomIdPartType.Sequence;

    public string Generate(CustomIdPartDefinition part, CustomIdGenerationContext context)
    {
        if (int.TryParse(part.Value, out var length) && length > 0)
        {
            return context.SequenceNumber.ToString($"D{length}");
        }

        return context.SequenceNumber.ToString();
    }
}
