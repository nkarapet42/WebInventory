namespace WebInventory.Web.Services;

public interface ISalesforceClient
{
    Task<SalesforceCustomerResult> CreateCustomerAsync(
        SalesforceCustomer customer,
        CancellationToken cancellationToken = default);
}

public sealed record SalesforceCustomer(
    string CompanyName,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? JobTitle);

public sealed record SalesforceCustomerResult(string AccountId, string ContactId);
