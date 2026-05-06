namespace WebInventory.Application.Models;

public record AccessChangeResult(bool Succeeded, string? ErrorMessage)
{
    public static AccessChangeResult Success() => new(true, null);
    public static AccessChangeResult Failed(string message) => new(false, message);
}
