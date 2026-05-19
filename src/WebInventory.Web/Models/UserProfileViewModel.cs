namespace WebInventory.Web.Models;

public class UserProfileViewModel
{
    public required string UserId { get; set; }
    public required string DisplayName { get; set; }
    public string? Email { get; set; }
    public bool IsCurrentUser { get; set; }
    public IReadOnlyList<UserInventoryRowViewModel> OwnedInventories { get; set; } = [];
    public IReadOnlyList<UserInventoryRowViewModel> WritableInventories { get; set; } = [];
}

public class UserInventoryRowViewModel
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string? CategoryName { get; set; }
    public required string OwnerName { get; set; }
    public int ItemCount { get; set; }
    public string AccessMode { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
