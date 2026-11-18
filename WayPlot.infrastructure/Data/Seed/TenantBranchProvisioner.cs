using Microsoft.EntityFrameworkCore;
using WayPlot.domain.Entities;
using WayPlot.infrastructure.Data;

namespace WayPlot.infrastructure.Data.Seed;

public sealed record BranchProvisionResult(int BranchesEnsured, int AssignmentsSet);

/// <summary>Idempotently ensures the Tier C demo branches without overwriting staff assignments.</summary>
public static class TenantBranchProvisioner
{
    public static async Task<BranchProvisionResult> EnsureTierCAsync(
        TenantDbContext db,
        CancellationToken ct = default)
    {
        var branches = await db.Branches.IgnoreQueryFilters().ToListAsync(ct);
        var mandaluyong = FindSingle(branches, "Mandaluyong");
        var antipolo = FindSingle(branches, "Antipolo");
        var ensured = 0;

        if (mandaluyong is null)
        {
            mandaluyong = new Branch { Name = "Mandaluyong" };
            db.Branches.Add(mandaluyong);
            ensured++;
        }
        else if (mandaluyong.IsDeleted)
        {
            mandaluyong.IsDeleted = false;
            ensured++;
        }

        if (antipolo is null)
        {
            db.Branches.Add(new Branch { Name = "Antipolo" });
            ensured++;
        }
        else if (antipolo.IsDeleted)
        {
            antipolo.IsDeleted = false;
            ensured++;
        }

        if (ensured > 0)
            await db.SaveChangesAsync(ct);

        var manager = await FindStaffAsync(db, "mgr.mandaluyong.c", ct);
        var ana = await FindStaffAsync(db, "ana.c", ct);
        if (manager is null || ana is null)
            throw new InvalidOperationException(
                "Tier C branch provisioning requires Staff rows for mgr.mandaluyong.c and ana.c.");

        var assignmentsSet = 0;
        foreach (var staff in new[] { manager, ana })
        {
            if (staff.BranchId is null)
            {
                staff.BranchId = mandaluyong.Id;
                assignmentsSet++;
            }
            else if (staff.BranchId != mandaluyong.Id)
            {
                throw new InvalidOperationException(
                    $"Tier C branch provisioning will not overwrite {staff.Username}'s existing BranchId {staff.BranchId}. Assign the staff member to Mandaluyong explicitly.");
            }
        }

        if (assignmentsSet > 0)
            await db.SaveChangesAsync(ct);

        return new BranchProvisionResult(ensured, assignmentsSet);
    }

    private static Branch? FindSingle(IEnumerable<Branch> branches, string name)
    {
        var matches = branches.Where(branch =>
            string.Equals(branch.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count > 1)
            throw new InvalidOperationException($"Multiple tenant Branches rows exist for {name}.");
        return matches.SingleOrDefault();
    }

    private static Task<Staff?> FindStaffAsync(
        TenantDbContext db,
        string username,
        CancellationToken ct) =>
        db.Staffs.IgnoreQueryFilters().SingleOrDefaultAsync(
            staff => staff.Username == username && !staff.IsDeleted,
            ct);
}
