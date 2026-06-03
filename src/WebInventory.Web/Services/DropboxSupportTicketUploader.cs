using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace WebInventory.Web.Services;

public class DropboxSupportTicketUploader : ISupportTicketUploader
{
    private readonly HttpClient _httpClient;
    private readonly DropboxOptions _options;

    public DropboxSupportTicketUploader(HttpClient httpClient, IOptions<DropboxOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task UploadAsync(SupportTicketDocument ticket, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            throw new InvalidOperationException("Dropbox support-ticket upload is not configured.");
        }

        var fileName = $"support-ticket-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.json";
        var folder = "/" + _options.SupportTicketFolder.Trim('/');
        var dropboxArguments = JsonSerializer.Serialize(new
        {
            path = $"{folder}/{fileName}",
            mode = "add",
            autorename = true,
            mute = false,
            strict_conflict = false
        });
        var payload = JsonSerializer.Serialize(ticket, new JsonSerializerOptions { WriteIndented = true });

        await EnsureFolderExistsAsync(folder, cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://content.dropboxapi.com/2/files/upload");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
        request.Headers.Add("Dropbox-API-Arg", dropboxArguments);
        request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(payload));
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Dropbox rejected the support ticket upload with status {(int)response.StatusCode}: {responseBody}");
        }
    }

    private async Task EnsureFolderExistsAsync(string folder, CancellationToken cancellationToken)
    {
        var currentPath = string.Empty;
        foreach (var segment in folder.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            currentPath += $"/{segment}";
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.dropboxapi.com/2/files/create_folder_v2");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { path = currentPath, autorename = false }),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                continue;
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict
                && responseBody.Contains("conflict/folder", StringComparison.Ordinal))
            {
                continue;
            }

            throw new InvalidOperationException(
                $"Dropbox rejected support-ticket folder creation with status {(int)response.StatusCode}: {responseBody}");
        }
    }
}
