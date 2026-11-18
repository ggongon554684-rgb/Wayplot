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
    private const string ExpectedLastMigration = "20261118090246_AddDealTermination";

    public async Task MigrateAllAsync(CancellationToken ct = default)
    {
        var tenants = await master.Tenants
            .Include(tenant => tenant.Plan)
            .Where(t => t.Status != TenantStatus.Suspended)
            .ToListAsync(ct);

        foreach (var tenant in tenants)
        {
            var step = "inspect migration state";
            try
            {
                await using var db = factory.Create(TenantConnection.For(tenant));

                var discovered = db.Database.GetMigrations().ToArray();
                var lastMigration = discovered.LastOrDefault();
                log.LogInformation(
                    "Tenant {Tenant}: discovered migration IDs [{DiscoveredMigrations}]",
                    tenant.Name,
                    string.Join(", ", discovered));
                if (!string.Equals(lastMigration, ExpectedLastMigration, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        $"Tenant migration discovery mismatch: expected last migration {ExpectedLastMigration}, found {lastMigration ?? "none"}. " +
                        $"Discovered migration IDs: [{string.Join(", ", discovered)}].");

                var appliedBefore = 0;
                var pending = Array.Empty<string>();
                var boundaryAppliedCount = (int?)null;
                if (await db.Database.CanConnectAsync(ct))
                {
                    appliedBefore = (await db.Database.GetAppliedMigrationsAsync(ct)).Count();
                    pending = (await db.Database.GetPendingMigrationsAsync(ct)).ToArray();
                }
                else
                {
                    // A missing database has no applied migration table yet; all
                    // code-discovered migrations are pending and MigrateAsync will create it.
                    pending = discovered;
                }

                log.LogInformation(
                    "Tenant {Tenant}: discovered {Discovered} migrations, applied {Applied}, pending {Pending}",
                    tenant.Name, discovered.Length, appliedBefore, pending.Length);

                if (pending.Length == 0 && discovered.Length > appliedBefore)
                    throw new InvalidOperationException(
                        $"EF reported no pending migrations although {discovered.Length} were discovered and only {appliedBefore} are applied.");

                if (pending.Length > 0)
                {
                    log.LogInformation(
                        "Tenant {Tenant} has {PendingCount} pending migration(s): {PendingMigrations}",
                        tenant.Name,
                        pending.Length,
                        string.Join(", ", pending));

                    // Part B adds Staff FKs, so existing databases must reach the
                    // provisioning boundary before the Staff rows are reconciled.
                    if (pending.Contains(StaffProvisioningBoundary, StringComparer.Ordinal))
                    {
                        step = "migrate to staff-provisioning boundary";
                        await db.Database.MigrateAsync(StaffProvisioningBoundary, ct);
                        var appliedAtBoundary = (await db.Database.GetAppliedMigrationsAsync(ct)).Count();
                        log.LogInformation(
                            "Tenant {Tenant}: migrated from {From} to {To}",
                            tenant.Name, appliedBefore, appliedAtBoundary);
                        boundaryAppliedCount = appliedAtBoundary;
                    }
                }

                step = "provision staff";
                var staffProvisioned = await TenantStaffProvisioner.ProvisionAsync(master, db, tenant.Id, ct);
                log.LogInformation("Tenant {Tenant}: {Count} staff provisioned", tenant.Name, staffProvisioned);

                step = "ensure branches";
                if (tenant.Plan?.Tier == Tier.C)
                {
                    var branches = await TenantBranchProvisioner.EnsureTierCAsync(db, ct);
                    log.LogInformation(
                        "Tenant {Tenant}: branches ensured ({BranchCount}), assignments set ({AssignmentCount})",
                        tenant.Name, branches.BranchesEnsured, branches.AssignmentsSet);
                }
                else
                {
                    log.LogInformation("Tenant {Tenant}: branches ensured (skipped; not Tier C)", tenant.Name);
                }

                if (pending.Length > 0)
                {
                    step = "migrate remaining pending migrations";
                    await db.Database.MigrateAsync(ct);
                }

                var appliedAfter = (await db.Database.GetAppliedMigrationsAsync(ct)).Count();
                var pendingAfter = (await db.Database.GetPendingMigrationsAsync(ct)).ToArray();
                var appliedMigrationIds = (await db.Database.GetAppliedMigrationsAsync(ct)).ToHashSet(StringComparer.Ordinal);
                if (pendingAfter.Length > 0)
                    throw new InvalidOperationException(
                        $"Tenant still has {pendingAfter.Length} pending migration(s) after migration attempt: {string.Join(", ", pendingAfter)}.");
                if (!appliedMigrationIds.Contains(lastMigration!))
                    throw new InvalidOperationException(
                        $"Tenant migration completed without applying the last migration {lastMigration}.");
                if (discovered.Length > appliedAfter)
                    throw new InvalidOperationException(
                        $"Tenant reports {appliedAfter} applied migrations but {discovered.Length} were discovered.");

                if (pending.Length > 0)
                {
                    var from = boundaryAppliedCount ?? appliedBefore;
                    log.LogInformation(
                        "Tenant {Tenant}: migrated from {From} to {To}",
                        tenant.Name, from, appliedAfter);
                }
                else if (boundaryAppliedCount is null)
                {
                    log.LogInformation(
                        "Tenant {Tenant}: migrated from {From} to {To}",
                        tenant.Name, appliedBefore, appliedAfter);
                }

                tenant.SchemaVersion = appliedAfter;
                tenant.Status = TenantStatus.Active;
                log.LogInformation("Tenant {Tenant} is on schema version {Version}", tenant.Name, tenant.SchemaVersion);
            }
            catch (Exception ex)
            {
                tenant.Status = TenantStatus.Failed;
                log.LogError(ex, "Tenant {Tenant} failed during step {Step}", tenant.Name, step);
            }
        }

        await master.SaveChangesAsync(ct);
    }
}
