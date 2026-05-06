using System.Security.Cryptography;
using WebInventory.Application.Interfaces;
using WebInventory.Application.Models;
using WebInventory.Domain.Enums;

namespace WebInventory.Infrastructure.Services.CustomId;

public class Random32BitGenerator : IIdPartGenerator
{
    public CustomIdPartType PartType => CustomIdPartType.Random32Bit;

    public string Generate(CustomIdPartDefinition part, CustomIdGenerationContext context)
    {
        var bytes = RandomNumberGenerator.GetBytes(4);
        var value = BitConverter.ToUInt32(bytes, 0);
        return value.ToString("X8");
    }
}
