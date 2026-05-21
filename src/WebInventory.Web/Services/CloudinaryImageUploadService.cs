using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace WebInventory.Web.Services;

public class CloudinaryImageUploadService : IImageUploadService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp"
    };

    private readonly CloudinaryOptions _options;
    private readonly Cloudinary? _cloudinary;

    public CloudinaryImageUploadService(IOptions<CloudinaryOptions> options)
    {
        _options = options.Value;
        _cloudinary = CreateClient(_options);
    }

    public bool CanUpload => _cloudinary is not null;

    public async Task<string> UploadInventoryImageAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (_cloudinary is null)
        {
            throw new InvalidOperationException("Cloudinary is not configured.");
        }

        if (file.Length == 0)
        {
            throw new InvalidOperationException("The selected image is empty.");
        }

        if (file.Length > _options.MaxImageBytes)
        {
            throw new InvalidOperationException($"The selected image must be smaller than {_options.MaxImageBytes / 1024 / 1024} MB.");
        }

        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            throw new InvalidOperationException("Only JPG, PNG, GIF, and WEBP images are allowed.");
        }

        await using var stream = file.OpenReadStream();
        var upload = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = _options.Folder,
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false
        };

        var result = await _cloudinary.UploadAsync(upload, cancellationToken);
        if (result.StatusCode is not System.Net.HttpStatusCode.OK and not System.Net.HttpStatusCode.Created)
        {
            throw new InvalidOperationException(result.Error?.Message ?? "Cloudinary rejected the upload.");
        }

        return result.SecureUrl?.ToString()
            ?? result.Url?.ToString()
            ?? throw new InvalidOperationException("Cloudinary did not return an image URL.");
    }

    private static Cloudinary? CreateClient(CloudinaryOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Url))
        {
            return CreateClientFromUrl(options.Url);
        }

        var environmentUrl = Environment.GetEnvironmentVariable("CLOUDINARY_URL");
        if (!string.IsNullOrWhiteSpace(environmentUrl))
        {
            return CreateClientFromUrl(environmentUrl);
        }

        if (string.IsNullOrWhiteSpace(options.CloudName)
            || string.IsNullOrWhiteSpace(options.ApiKey)
            || string.IsNullOrWhiteSpace(options.ApiSecret))
        {
            return null;
        }

        return new Cloudinary(new Account(options.CloudName, options.ApiKey, options.ApiSecret));
    }

    private static Cloudinary? CreateClientFromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || !string.Equals(uri.Scheme, "cloudinary", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(uri.Host))
        {
            return null;
        }

        var credentials = uri.UserInfo.Split(':', 2);
        if (credentials.Length != 2)
        {
            return null;
        }

        return new Cloudinary(new Account(uri.Host, credentials[0], credentials[1]));
    }
}
