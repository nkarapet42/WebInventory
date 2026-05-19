using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebInventory.Domain.Constants;

namespace WebInventory.Web.Controllers;

[Authorize]
public class SessionController : Controller
{
    [HttpGet]
    public IActionResult State()
    {
        return Json(new
        {
            isAuthenticated = User.Identity?.IsAuthenticated == true,
            isAdmin = User.IsInRole(RoleNames.Admin)
        });
    }
}
