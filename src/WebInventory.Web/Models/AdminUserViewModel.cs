namespace WebInventory.Web.Models;

public class AdminUserViewModel
{
    public required string Id { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool IsBlocked { get; set; }
    public bool IsAdmin { get; set; }
}
