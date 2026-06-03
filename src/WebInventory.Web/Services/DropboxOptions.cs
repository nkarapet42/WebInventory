namespace WebInventory.Web.Services;

public class DropboxOptions
{
    public string AccessToken { get; set; } = string.Empty;
    public string SupportTicketFolder { get; set; } = "/webinventory-support-tickets";
}
