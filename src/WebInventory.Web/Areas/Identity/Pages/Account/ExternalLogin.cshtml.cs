using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebInventory.Domain.Identity;

namespace WebInventory.Web.Areas.Identity.Pages.Account;

public class ExternalLoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ExternalLoginModel> _logger;

    public ExternalLoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<ExternalLoginModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [TempData]
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [EmailAddress]
        public string? Email { get; set; }
    }

    public IActionResult OnGet()
    {
        return RedirectToPage("./Login");
    }

    public IActionResult OnPost(string provider, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            ErrorMessage = "External login provider is missing.";
            return RedirectToPage("./Login");
        }

        var redirectUrl = Url.Page("./ExternalLogin", "Callback", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }

    public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl = null, string? remoteError = null)
    {
        returnUrl ??= Url.Content("~/");
        if (remoteError is not null)
        {
            ErrorMessage = $"External provider error: {remoteError}";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            ErrorMessage = "Could not read the external login information.";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        var signInResult = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider,
            info.ProviderKey,
            isPersistent: false,
            bypassTwoFactor: true);
        if (signInResult.Succeeded)
        {
            _logger.LogInformation("{Provider} user logged in.", info.LoginProvider);
            return LocalRedirect(returnUrl);
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            ErrorMessage = $"{info.LoginProvider} did not return an email address.";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        Input.Email = email;
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                ErrorMessage = BuildErrorMessage(createResult);
                return Page();
            }
        }

        var addLoginResult = await _userManager.AddLoginAsync(user, info);
        if (!addLoginResult.Succeeded)
        {
            ErrorMessage = BuildErrorMessage(addLoginResult);
            return Page();
        }

        await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
        _logger.LogInformation("User {Email} logged in with {Provider}.", email, info.LoginProvider);
        return LocalRedirect(returnUrl);
    }

    private static string BuildErrorMessage(IdentityResult result)
    {
        return string.Join(" ", result.Errors.Select(error => error.Description));
    }
}
