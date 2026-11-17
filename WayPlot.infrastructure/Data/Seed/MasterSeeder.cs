using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WayPlot.domain.Common;
using WayPlot.domain.Master;
using WayPlot.infrastructure.Identity;

namespace WayPlot.infrastructure.Data.Seed;

/// <summary>Creates the Master database, plans, three demo tenants and the demo logins. Safe to run repeatedly.</summary>
public static class MasterSeeder
{
    public const string DemoPassword = "Demo#2026";

    public static async Task SeedAsync(IServiceProvider services, string serverName, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<MasterDbContext>();
        await db.Database.MigrateAsync(ct);

        var roles = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in Roles.All)
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole<Guid>(role));

        if (!await db.Plans.AnyAsync(ct))
        {
            db.Plans.AddRange(
                new Plan { Name = "Tier A", Tier = Tier.A, MonthlyPrice = 1500 },
                new Plan { Name = "Tier B", Tier = Tier.B, MonthlyPrice = 3000 },
                new Plan { Name = "Tier C", Tier = Tier.C, MonthlyPrice = 5000 });
            await db.SaveChangesAsync(ct);
        }

        var plans = await db.Plans.ToDictionaryAsync(p => p.Tier, ct);

        if (!await db.Tenants.AnyAsync(ct))
        {
            var demo = new[]
            {
                (Name: "Santos Land Brokers", Database: "WayPlot_TenantA", Tier: Tier.A),
                (Name: "Reyes Realty", Database: "WayPlot_TenantB", Tier: Tier.B),
                (Name: "Bayside Developers", Database: "WayPlot_TenantC", Tier: Tier.C)
            };

            foreach (var d in demo)
            {
                var tenant = new Tenant
                {
                    Name = d.Name,
                    PlanId = plans[d.Tier].Id,
                    ServerName = serverName,
                    DatabaseName = d.Database,
                    ExpiresAtUtc = DateTime.UtcNow.AddYears(1)
                };
                db.Tenants.Add(tenant);
                db.Subscriptions.Add(new Subscription
                {
                    TenantId = tenant.Id,
                    PlanId = tenant.PlanId,
                    ExpiresAtUtc = tenant.ExpiresAtUtc!.Value,
                    PaymentReference = "DEMO"
                });
            }
            await db.SaveChangesAsync(ct);
        }

        var tenants = await db.Tenants.Include(t => t.Plan).ToListAsync(ct);
        Guid TenantOf(Tier tier) => tenants.First(t => t.Plan!.Tier == tier).Id;

        var accounts = new (string User, string Name, string Role, Tier? Tier)[]
        {
            ("superadmin", "Platform Admin", Roles.SuperAdmin, null),
            ("admin.a", "Admin (Tenant A)", Roles.TenantAdmin, Tier.A),
            ("ana.a", "Ana", Roles.Agent, Tier.A),
            ("ben.a", "Ben", Roles.Agent, Tier.A),
            ("admin.b", "Admin (Tenant B)", Roles.TenantAdmin, Tier.B),
            ("ben.b", "Ben", Roles.Agent, Tier.B),
            ("admin.c", "Admin (Tenant C)", Roles.TenantAdmin, Tier.C),
            ("mgr.mandaluyong.c", "Branch Manager", Roles.BranchManager, Tier.C),
            ("ana.c", "Ana", Roles.Agent, Tier.C)
        };

        var users = sp.GetRequiredService<UserManager<AppUser>>();
        foreach (var a in accounts)
        {
            if (await users.FindByNameAsync(a.User) is not null) continue;

            var user = new AppUser
            {
                UserName = a.User,
                Email = $"{a.User}@wayplot.local",
                EmailConfirmed = true,
                DisplayName = a.Name,
                TenantId = a.Tier is null ? null : TenantOf(a.Tier.Value)
            };

            var result = await users.CreateAsync(user, DemoPassword);
            if (result.Succeeded) await users.AddToRoleAsync(user, a.Role);
        }
    }
}
