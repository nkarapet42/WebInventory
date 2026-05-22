using WebInventory.Domain.Entities;

namespace WebInventory.Application.Interfaces;

public interface ICustomIdGenerator
{
    Task<string> GenerateAsync(Inventory inventory);

    Task<bool> MatchesCurrentPatternAsync(Inventory inventory, string customId);
}
