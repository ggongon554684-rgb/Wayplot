using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using WayPlot.infrastructure.Common;
using WayPlot.infrastructure.Tenancy;

namespace WayPlot.infrastructure.Data;

// Used only by Add-Migration and Update-Database. Override the defaults with environment variables if needed.

public sealed class MasterDesignTimeFactory : IDesignTimeDbContextFactory<MasterDbContext>
{
    public MasterDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("WAYPLOT_MASTER_CONN")
                 ?? "Server=(localdb)\\MSSQLLocalDB;Database=WayPlot_Master;Trusted_Connection=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<MasterDbContext>().UseSqlServer(cs).Options;
        return new MasterDbContext(options);
    }
}

public sealed class TenantDesignTimeFactory : IDesignTimeDbContextFactory<TenantDbContext>
{
    public TenantDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("WAYPLOT_TENANT_CONN")
                 ?? "Server=(localdb)\\MSSQLLocalDB;Database=WayPlot_Template;Trusted_Connection=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<TenantDbContext>().UseSqlServer(cs).Options;
        return new TenantDbContext(options, SessionContext.SystemUser, new SystemClock());
    }
}
