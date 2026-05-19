using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using WebInventory.Domain.Identity;

namespace WebInventory.Web.Services;

public class DatabaseRoleClaimsTransformation : IClaimsTransformation
{
    private readonly UserManager<ApplicationUser> _userManager;

    public DatabaseRoleClaimsTransformation(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity?.IsAuthenticated != true)
        {
            return principal;
        }

        var user = await _userManager.GetUserAsync(principal);
        if (user is null || await _userManager.IsLockedOutAsync(user))
        {
            return principal;
        }

        var roleClaimType = identity.RoleClaimType;
        var claims = identity.Claims
            .Where(claim => claim.Type != roleClaimType)
            .ToList();

        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(roleClaimType, role)));

        var refreshedIdentity = new ClaimsIdentity(
            claims,
            identity.AuthenticationType,
            identity.NameClaimType,
            roleClaimType);

        return new ClaimsPrincipal(refreshedIdentity);
    }
}
