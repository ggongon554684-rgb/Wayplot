using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.infrastructure.Data;
using WayPlot.infrastructure.Identity;

namespace WayPlot.api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(UserManager<AppUser> users, MasterDbContext master) : ControllerBase
{
    private const string Invalid = "Invalid username or password.";

    /// <summary>Username and password only. Master finds the tenant, so the screen never asks for one.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResult>> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await users.FindByNameAsync(request.Username);
        if (user is null || !user.IsActive || await users.IsLockedOutAsync(user))
            return Unauthorized(Invalid);

        if (!await users.CheckPasswordAsync(user, request.Password))
        {
            await users.AccessFailedAsync(user);
            return Unauthorized(Invalid);
        }
        await users.ResetAccessFailedCountAsync(user);

        var tenant = user.TenantId is null
            ? null
            : await master.Tenants.Include(t => t.Plan).FirstOrDefaultAsync(t => t.Id == user.TenantId, ct);

        var role = (await users.GetRolesAsync(user)).FirstOrDefault() ?? string.Empty;
        var readOnly = tenant is not null &&
            (tenant.Status != TenantStatus.Active || (tenant.ExpiresAtUtc is { } expiry && expiry < DateTime.UtcNow));

        return new LoginResult(user.Id, user.UserName!, user.DisplayName, role,
            tenant?.Id, tenant?.Name, tenant?.Plan?.Tier, tenant?.DatabaseName, readOnly);
    }
}