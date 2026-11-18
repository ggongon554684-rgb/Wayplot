using Microsoft.Extensions.DependencyInjection;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Events;
using WayPlot.domain.Interfaces;
using WayPlot.domain.Repositories;
using WayPlot.domain.Services.Admin;
using WayPlot.domain.Services.DataCollection;
using WayPlot.domain.Services.MainTransaction;
using WayPlot.infrastructure.Common;
using WayPlot.infrastructure.Data.Seed;
using WayPlot.infrastructure.Events;
using WayPlot.infrastructure.Repositories;
using WayPlot.infrastructure.Tenancy;

namespace WayPlot.infrastructure;

public static class TenantClientExtensions
{
    /// <summary>Registers only what the WinForms app needs. No Master connection string, no Identity, no migrator.
    /// serverName comes from "Tenancy:ServerName" in the app's appsettings.json.</summary>
    public static IServiceCollection AddWayPlotTenantClient(this IServiceCollection services, string serverName)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<IPipelineStageProvider, DefaultPipelineStageProvider>();
        services.AddSingleton<SessionContext>();
        services.AddSingleton<ICurrentUser>(sp => sp.GetRequiredService<SessionContext>());
        services.AddSingleton<TenantAccessPolicy>();
        services.AddSingleton(new TenantSession(serverName));
        services.AddSingleton<ITenantDbContextFactory, TenantDbContextFactory>();
        services.AddSingleton<ITenantUnitOfWorkFactory, TenantUnitOfWorkFactory>();
        services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddSingleton<SessionStarter>();

        // The plan's feature switches. Transient, so every resolve after login reads the signed-in tenant's tier.
        services.AddTransient<ITenantFeatures>(sp =>
            new TenantFeatures(sp.GetRequiredService<SessionContext>().Tier ?? Tier.A));

        // The services keep no state: each operation opens its own unit of work.
        services.AddSingleton<ContactService>();
        services.AddSingleton<ContactImportService>();
        services.AddSingleton<ParcelService>();
        services.AddSingleton<ParcelPhotoService>();
        services.AddSingleton<LeadService>();
        services.AddSingleton<InteractionService>();
        services.AddSingleton<CommissionService>();
        services.AddSingleton<DealService>();
        services.AddSingleton<StaffService>();
        services.AddSingleton<CompanyProfileService>();
        services.AddSingleton<TenantSeeder>();
        return services;
    }
}
