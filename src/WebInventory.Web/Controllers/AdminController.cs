using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebInventory.Domain.Constants;
using WebInventory.Domain.Identity;
using WebInventory.Web.Models;

namespace WebInventory.Web.Controllers;

[Authorize(Roles = RoleNames.Admin)]
public class AdminController : Controller
{
    private static readonly DateTimeOffset BlockedUntil = DateTimeOffset.MaxValue;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .OrderBy(u => u.UserName)
            .ThenBy(u => u.Email)
            .ToListAsync();

        var model = new List<AdminUserViewModel>(users.Count);
        foreach (var user in users)
        {
            model.Add(new AdminUserViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                IsBlocked = user.LockoutEnd is not null && user.LockoutEnd > DateTimeOffset.UtcNow,
                IsAdmin = await _userManager.IsInRoleAsync(user, RoleNames.Admin)
            });
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Block(string id)
    {
        var user = await FindUserOrNullAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        await _userManager.SetLockoutEnabledAsync(user, true);
        await _userManager.SetLockoutEndDateAsync(user, BlockedUntil);

        if (IsCurrentUser(user))
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unblock(string id)
    {
        var user = await FindUserOrNullAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        await _userManager.SetLockoutEndDateAsync(user, null);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAdmin(string id)
    {
        var user = await FindUserOrNullAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (!await _userManager.IsInRoleAsync(user, RoleNames.Admin))
        {
            await _userManager.AddToRoleAsync(user, RoleNames.Admin);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveAdmin(string id)
    {
        var user = await FindUserOrNullAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (await _userManager.IsInRoleAsync(user, RoleNames.Admin))
        {
            await _userManager.RemoveFromRoleAsync(user, RoleNames.Admin);
        }

        if (IsCurrentUser(user))
        {
            await _signInManager.RefreshSignInAsync(user);
            return RedirectToAction("Index", "Home");
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await FindUserOrNullAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var deletingCurrentUser = IsCurrentUser(user);
        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            TempData["AdminError"] = string.Join(" ", result.Errors.Select(error => error.Description));
            return RedirectToAction(nameof(Index));
        }

        if (deletingCurrentUser)
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        return RedirectToAction(nameof(Index));
    }

    private bool IsCurrentUser(ApplicationUser user)
    {
        return string.Equals(_userManager.GetUserId(User), user.Id, StringComparison.Ordinal);
    }

    private async Task<ApplicationUser?> FindUserOrNullAsync(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? null : await _userManager.FindByIdAsync(id);
    }
}
