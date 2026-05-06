using System.Security.Cryptography;
using WebInventory.Application.Interfaces;
using WebInventory.Application.Models;
using WebInventory.Domain.Enums;

namespace WebInventory.Infrastructure.Services.CustomId;

public class Random20BitGenerator : IIdPartGenerator
{
    public CustomIdPartType PartType => CustomIdPartType.Random20Bit;

    public string Generate(CustomIdPartDefinition part, CustomIdGenerationContext context)
    {
        var value = RandomNumberGenerator.GetInt32(1 << 20);
        return value.ToString("X5");
    }
}
