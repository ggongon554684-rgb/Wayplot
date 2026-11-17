using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Entities;

namespace WayPlot.infrastructure.Tenancy;

/// <summary>Runs once after a successful login: fills SessionContext, points TenantSession at the tenant database,
/// and makes sure the user has a Staff row there (Staff.Id = the login's UserId).</summary>
public sealed class SessionStarter(SessionContext session, TenantSession tenant, ITenantDbContextFactory dbFactory)
{
    public async Task<RuleResult> StartAsync(LoginResult login)
    {
        if (login.TenantId is null || string.IsNullOrWhiteSpace(login.DatabaseName))
            return RuleResult.Fail("This account is not linked to a tenant database.");

        // Filled first so the audit fields on the Staff row carry this user's name.
        session.UserId = login.UserId;
        session.UserName = login.Username;
        session.TenantId = login.TenantId;
        session.BranchId = null;
        session.Tier = login.Tier;
        session.Role = login.Role;
        tenant.Start(login.DatabaseName);

        try
        {
            await using var db = dbFactory.Create(tenant.ConnectionString);

            // IgnoreQueryFilters: a removed (soft-deleted) staff row still owns the unique UserId index.
            var staff = await db.Staffs.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.UserId == login.UserId);

            if (staff is null)
            {
                db.Staffs.Add(new Staff
                {
                    Id = login.UserId,
                    UserId = login.UserId,
                    Username = login.Username,
                    FullName = login.DisplayName,
                    Role = login.Role,
                    IsActive = true
                });
                await db.SaveChangesAsync();
            }
            else if (staff.IsDeleted || !staff.IsActive)
            {
                End();
                return RuleResult.Fail("This staff account is deactivated. Ask your Tenant Admin.");
            }
            else if (staff.Username != login.Username || staff.FullName != login.DisplayName || staff.Role != login.Role)
            {
                staff.Username = login.Username;
                staff.FullName = login.DisplayName;
                staff.Role = login.Role;
                await db.SaveChangesAsync();
            }

            return RuleResult.Success();
        }
        catch (Exception ex) when (ex is SqlException or DbUpdateException)
        {
            End();
            return RuleResult.Fail("Could not open the tenant database. Check that LocalDB is running and that Tenancy:ServerName is correct.");
        }
    }

    /// <summary>Sign-out, or a failed start. Clears everything the start filled.</summary>
    public void End()
    {
        session.UserId = null;
        session.UserName = "anonymous";
        session.TenantId = null;
        session.BranchId = null;
        session.Tier = null;
        session.Role = string.Empty;
        tenant.Clear();
    }
}