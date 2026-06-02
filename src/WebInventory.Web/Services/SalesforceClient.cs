using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace WebInventory.Web.Services;

public class SalesforceClient : ISalesforceClient
{
    private readonly HttpClient _httpClient;
    private readonly SalesforceOptions _options;
    private readonly ILogger<SalesforceClient> _logger;

    public SalesforceClient(
        HttpClient httpClient,
        IOptions<SalesforceOptions> options,
        ILogger<SalesforceClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SalesforceCustomerResult> CreateCustomerAsync(
        SalesforceCustomer customer,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var token = await GetAccessTokenAsync(cancellationToken);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{token.InstanceUrl}/services/data/v{_options.ApiVersion}/composite")
        {
            Content = JsonContent.Create(new
            {
                allOrNone = true,
                compositeRequest = new object[]
                {
                    new
                    {
                        method = "POST",
                        url = $"/services/data/v{_options.ApiVersion}/sobjects/Account",
                        referenceId = "account",
                        body = new
                        {
                            Name = customer.CompanyName
                        }
                    },
                    new
                    {
                        method = "POST",
                        url = $"/services/data/v{_options.ApiVersion}/sobjects/Contact",
                        referenceId = "contact",
                        body = new
                        {
                            AccountId = "@{account.id}",
                            FirstName = customer.FirstName,
                            LastName = customer.LastName,
                            Email = customer.Email,
                            Phone = customer.Phone,
                            Title = customer.JobTitle
                        }
                    }
                }
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(token.TokenType, token.AccessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Salesforce composite request failed with status code {StatusCode}.", response.StatusCode);
            throw new SalesforceException("Salesforce rejected the customer information.");
        }

        var composite = JsonSerializer.Deserialize<CompositeResponse>(responseBody)
            ?? throw new SalesforceException("Salesforce returned an invalid response.");
        var account = composite.CompositeResponseItems.FirstOrDefault(item => item.ReferenceId == "account");
        var contact = composite.CompositeResponseItems.FirstOrDefault(item => item.ReferenceId == "contact");
        if (account?.HttpStatusCode is not 201 || contact?.HttpStatusCode is not 201)
        {
            _logger.LogWarning(
                "Salesforce record creation failed. Account status: {AccountStatus}; Contact status: {ContactStatus}.",
                account?.HttpStatusCode,
                contact?.HttpStatusCode);
            throw new SalesforceException("Salesforce could not create the Account and linked Contact.");
        }

        return new SalesforceCustomerResult(
            account.Body?.Id ?? throw new SalesforceException("Salesforce did not return an Account ID."),
            contact.Body?.Id ?? throw new SalesforceException("Salesforce did not return a Contact ID."));
    }

    private async Task<OAuthToken> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsync(
            $"{_options.LoginUrl.TrimEnd('/')}/services/oauth2/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.ClientId!,
                ["client_secret"] = _options.ClientSecret!
            }),
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Salesforce OAuth request failed with status code {StatusCode}.", response.StatusCode);
            throw new SalesforceException("Could not authenticate with Salesforce.");
        }

        return await response.Content.ReadFromJsonAsync<OAuthToken>(cancellationToken: cancellationToken)
            ?? throw new SalesforceException("Salesforce returned an invalid authentication response.");
    }

    private void EnsureConfigured()
    {
        if (!Uri.TryCreate(_options.LoginUrl, UriKind.Absolute, out var loginUri)
            || loginUri.Scheme != Uri.UriSchemeHttps
            || (!string.Equals(loginUri.Host, "salesforce.com", StringComparison.OrdinalIgnoreCase)
                && !loginUri.Host.EndsWith(".salesforce.com", StringComparison.OrdinalIgnoreCase))
            || string.IsNullOrWhiteSpace(_options.ClientId)
            || string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            throw new SalesforceException("Salesforce integration is not configured.");
        }
    }

    private sealed class OAuthToken
    {
        [JsonPropertyName("access_token")]
        public required string AccessToken { get; init; }

        [JsonPropertyName("instance_url")]
        public required string InstanceUrl { get; init; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; init; } = "Bearer";
    }

    private sealed class CompositeResponse
    {
        [JsonPropertyName("compositeResponse")]
        public List<CompositeResponseItem> CompositeResponseItems { get; init; } = [];
    }

    private sealed class CompositeResponseItem
    {
        [JsonPropertyName("body")]
        public CompositeResponseBody? Body { get; init; }

        [JsonPropertyName("httpStatusCode")]
        public int HttpStatusCode { get; init; }

        [JsonPropertyName("referenceId")]
        public string? ReferenceId { get; init; }
    }

    private sealed class CompositeResponseBody
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }
    }
}

public class SalesforceException : Exception
{
    public SalesforceException(string message)
        : base(message)
    {
    }
}
