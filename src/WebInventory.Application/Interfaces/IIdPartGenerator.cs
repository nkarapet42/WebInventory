using WebInventory.Application.Models;
using WebInventory.Domain.Enums;

namespace WebInventory.Application.Interfaces;

public interface IIdPartGenerator
{
    CustomIdPartType PartType { get; }
    string Generate(CustomIdPartDefinition part, CustomIdGenerationContext context);
}
