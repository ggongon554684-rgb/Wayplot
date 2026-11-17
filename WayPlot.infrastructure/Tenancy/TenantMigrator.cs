using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WayPlot.domain.Common;
using WayPlot.infrastructure.Data;
using WayPlot.infrastructure.Data.Seed;

namespace WayPlot.infrastructure.Tenancy;

/// <summary>
/// Applies the tenant schema to every tenant database listed in Master. MigrateAsync also creates a
/// missing database, so this doubles as the development provisioner. One failure never stops the others.
/// </summary>
public sealed class TenantMigrator(MasterDbContext master, ITenantDbContextFactory factory, ILogger<TenantMigrator> log)
{
    private const string StaffProvisioningBoundary = "20261117123239_RunA_StaffBranchesAndIntegrity";

    public async Task MigrateAllAsync(CancellationToken ct = default)
    {
        var tenants = await master.Tenants.Where(t => t.Status != TenantStatus.Suspended).ToListAsync(ct);

        foreach (var tenant in tenants)
        {
            try
            {
                await using var db = factory.Create(TenantConnection.For(tenant));
                await db.Database.MigrateAsync(StaffProvisioningBoundary, ct);
                await TenantStaffProvisioner.ProvisionAsync(master, db, tenant.Id, ct);
                await db.Database.MigrateAsync(ct);
                tenant.SchemaVersion = (await db.Database.GetAppliedMigrationsAsync(ct)).Count();
                tenant.Status = TenantStatus.Active;
                log.LogInformation("Tenant {Tenant} is on schema version {Version}", tenant.Name, tenant.SchemaVersion);
            }
            catch (Exception ex)
            {
                tenant.Status = TenantStatus.Failed;
                log.LogError(ex, "Migration failed for tenant {Tenant}", tenant.Name);
            }
        }

        await master.SaveChangesAsync(ct);
    }
}
