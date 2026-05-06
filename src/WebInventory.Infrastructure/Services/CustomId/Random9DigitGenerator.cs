using System.Security.Cryptography;
using WebInventory.Application.Interfaces;
using WebInventory.Application.Models;
using WebInventory.Domain.Enums;

namespace WebInventory.Infrastructure.Services.CustomId;

public class Random9DigitGenerator : IIdPartGenerator
{
    public CustomIdPartType PartType => CustomIdPartType.Random9Digit;

    public string Generate(CustomIdPartDefinition part, CustomIdGenerationContext context)
    {
        var value = RandomNumberGenerator.GetInt32(0, 1_000_000_000);
        return value.ToString("D9");
    }
}
