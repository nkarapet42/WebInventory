using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebInventory.Domain.Constants;
using WebInventory.Domain.Identity;
using WebInventory.Web.Models;
using WebInventory.Web.Services;

namespace WebInventory.Web.Controllers;

[Authorize]
public class SalesforceController : Controller
{
    private readonly ISalesforceClient _salesforceClient;
    private readonly UserManager<ApplicationUser> _userManager;

    public SalesforceController(
        ISalesforceClient salesforceClient,
        UserManager<ApplicationUser> userManager)
    {
        _salesforceClient = salesforceClient;
        _userManager = userManager;
    }

    public async Task<IActionResult> CreateCustomer(string id)
    {
        var user = await FindAuthorizedUserAsync(id);
        if (user is null)
        {
            return Forbid();
        }

        if (HasSalesforceLink(user))
        {
            TempData["SalesforceSuccess"] = "This user is already linked to Salesforce.";
            return RedirectToAction("Profile", "Users", new { id = user.Id });
        }

        return View(new SalesforceCustomerViewModel
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCustomer(
        SalesforceCustomerViewModel model,
        CancellationToken cancellationToken)
    {
        var user = await FindAuthorizedUserAsync(model.UserId);
        if (user is null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (HasSalesforceLink(user))
        {
            TempData["SalesforceSuccess"] = "This user is already linked to Salesforce.";
            return RedirectToAction("Profile", "Users", new { id = user.Id });
        }

        try
        {
            var result = await _salesforceClient.CreateCustomerAsync(
                new SalesforceCustomer(
                    model.CompanyName.Trim(),
                    model.FirstName.Trim(),
                    model.LastName.Trim(),
                    model.Email.Trim(),
                    NullIfWhiteSpace(model.Phone),
                    NullIfWhiteSpace(model.JobTitle)),
                cancellationToken);
            user.SalesforceAccountId = result.AccountId;
            user.SalesforceContactId = result.ContactId;
            user.SalesforceSyncedAt = DateTime.UtcNow;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                throw new SalesforceException(
                    "Salesforce records were created, but WebInventory could not save the local link.");
            }

            TempData["SalesforceSuccess"] =
                $"Salesforce Account {result.AccountId} and linked Contact {result.ContactId} were created.";
            return RedirectToAction("Profile", "Users", new { id = user.Id });
        }
        catch (SalesforceException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Could not connect to Salesforce. Please try again.");
            return View(model);
        }
    }

    private async Task<ApplicationUser?> FindAuthorizedUserAsync(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var currentUserId = _userManager.GetUserId(User);
        if (!string.Equals(currentUserId, id, StringComparison.Ordinal)
            && !User.IsInRole(RoleNames.Admin))
        {
            return null;
        }

        return await _userManager.FindByIdAsync(id);
    }

    private static bool HasSalesforceLink(ApplicationUser user)
    {
        return !string.IsNullOrWhiteSpace(user.SalesforceAccountId)
            || !string.IsNullOrWhiteSpace(user.SalesforceContactId);
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
