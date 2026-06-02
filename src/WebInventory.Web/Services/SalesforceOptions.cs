namespace WebInventory.Web.Services;

public class SalesforceOptions
{
    public string LoginUrl { get; set; } = "https://login.salesforce.com";

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string ApiVersion { get; set; } = "66.0";
}
