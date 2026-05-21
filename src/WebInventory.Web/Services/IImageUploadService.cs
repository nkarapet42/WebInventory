namespace WebInventory.Web.Services;

public interface IImageUploadService
{
    bool CanUpload { get; }

    Task<string> UploadInventoryImageAsync(IFormFile file, CancellationToken cancellationToken);
}
