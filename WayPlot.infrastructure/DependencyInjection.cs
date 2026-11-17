using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WayPlot.domain.Events;
using WayPlot.domain.Interfaces;
using WayPlot.infrastructure.Common;
using WayPlot.infrastructure.Data;
using WayPlot.infrastructure.Events;
using WayPlot.infrastructure.Identity;
using WayPlot.infrastructure.Tenancy;

namespace WayPlot.infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddWayPlotInfrastructure(this IServiceCollection services, string masterConnectionString)
    {
        services.AddDbContext<MasterDbContext>(o => o.UseSqlServer(masterConnectionString));

        services.AddIdentityCore<AppUser>(o =>
            {
                o.Password.RequiredLength = 8;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<MasterDbContext>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<SessionContext>();
        services.AddSingleton<ICurrentUser>(sp => sp.GetRequiredService<SessionContext>());
        services.AddSingleton<ITenantDbContextFactory, TenantDbContextFactory>();
        services.AddScoped<TenantMigrator>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        return services;
    }
}
