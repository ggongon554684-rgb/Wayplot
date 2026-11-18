using Microsoft.EntityFrameworkCore;
using WayPlot.infrastructure.Data;

namespace WayPlot.infrastructure.Data.Seed;

/// <summary>Reconciles the tenant Staff table with the tenant's Master logins.</summary>
public static class TenantStaffProvisioner
{
    public static async Task<int> ProvisionAsync(
        MasterDbContext master,
        TenantDbContext tenant,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var logins = await master.Users
            .AsNoTracking()
            .Where(user => user.TenantId == tenantId)
            .Select(user => new
            {
                user.Id,
                user.UserName,
                user.DisplayName
            })
            .ToListAsync(ct);

        var loginIds = logins.Select(login => login.Id).ToList();
        var roleRows = await (
            from userRole in master.UserRoles
            join role in master.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where loginIds.Contains(userRole.UserId)
            select new { userRole.UserId, Role = role.Name })
            .ToListAsync(ct);

        var existing = await tenant.Staffs
            .IgnoreQueryFilters()
            .Where(staff => logins.Select(user => user.Id).Contains(staff.UserId))
            .ToListAsync(ct);

        var duplicate = existing
            .GroupBy(staff => staff.UserId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException(
                $"Tenant {tenantId} has more than one Staff row for login {duplicate.Key}. Resolve the duplicate before provisioning.");

        var byUserId = existing.ToDictionary(staff => staff.UserId);
        foreach (var login in logins)
        {
            if (!byUserId.TryGetValue(login.Id, out var staff))
            {
                staff = new WayPlot.domain.Entities.Staff
                {
                    Id = login.Id,
                    UserId = login.Id
                };
                tenant.Staffs.Add(staff);
            }

            staff.Username = login.UserName ?? string.Empty;
            staff.FullName = login.DisplayName;
            staff.Role = roleRows.FirstOrDefault(role => role.UserId == login.Id)?.Role ?? string.Empty;
            staff.IsActive = true;
            staff.IsDeleted = false;
        }

        await tenant.SaveChangesAsync(ct);
        return logins.Count;
    }
}
