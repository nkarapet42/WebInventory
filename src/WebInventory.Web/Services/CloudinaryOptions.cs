namespace WebInventory.Web.Services;

public class CloudinaryOptions
{
    public string? CloudName { get; set; }

    public string? ApiKey { get; set; }

    public string? ApiSecret { get; set; }

    public string? Url { get; set; }

    public string Folder { get; set; } = "webinventory/inventories";

    public long MaxImageBytes { get; set; } = 5 * 1024 * 1024;
}
