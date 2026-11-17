using Microsoft.EntityFrameworkCore;
using WayPlot.domain.Interfaces;
using WayPlot.infrastructure.Data;

namespace WayPlot.infrastructure.Tenancy;

public interface ITenantDbContextFactory
{
    TenantDbContext Create(string connectionString);
}

/// <summary>Builds a TenantDbContext for one tenant's database. Tenant contexts are never injected directly.</summary>
public sealed class TenantDbContextFactory(ICurrentUser user, IClock clock) : ITenantDbContextFactory
{
    public TenantDbContext Create(string connectionString)
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>().UseSqlServer(connectionString).Options;
        return new TenantDbContext(options, user, clock);
    }
}
