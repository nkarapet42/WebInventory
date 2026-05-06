using WebInventory.Application.Interfaces;
using WebInventory.Application.Models;
using WebInventory.Domain.Enums;

namespace WebInventory.Infrastructure.Services.CustomId;

public class GuidGenerator : IIdPartGenerator
{
    public CustomIdPartType PartType => CustomIdPartType.Guid;

    public string Generate(CustomIdPartDefinition part, CustomIdGenerationContext context)
    {
        return Guid.NewGuid().ToString("N");
    }
}
