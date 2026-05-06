using WebInventory.Domain.Enums;

namespace WebInventory.Application.Models;

public record CustomIdPartDefinition(CustomIdPartType Type, string? Value);
