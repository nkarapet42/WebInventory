using Microsoft.AspNetCore.Identity;

namespace WebInventory.Domain.Identity;

public class ApplicationUser : IdentityUser
{
    public string? SalesforceAccountId { get; set; }

    public string? SalesforceContactId { get; set; }

    public DateTime? SalesforceSyncedAt { get; set; }
}
