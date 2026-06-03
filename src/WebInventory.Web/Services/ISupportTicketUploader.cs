namespace WebInventory.Web.Services;

public interface ISupportTicketUploader
{
    Task UploadAsync(SupportTicketDocument ticket, CancellationToken cancellationToken = default);
}
