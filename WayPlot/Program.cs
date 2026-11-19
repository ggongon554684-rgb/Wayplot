using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WayPlot.domain.Contracts;
using WayPlot.domain.Interfaces;
using WayPlot.infrastructure;
using WayPlot.Presenters;
using WayPlot.Views;
using WayPlot.domain.Services.DataCollection;
using WayPlot.domain.Services.MainTransaction;
using WayPlot.domain.Services.Admin;
using WayPlot.UI;

namespace WayPlot;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = AppContext.BaseDirectory
        });
        var apiUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5039";
        var serverName = builder.Configuration["Tenancy:ServerName"] ?? "(localdb)\\MSSQLLocalDB";
        builder.Services.AddSingleton<Func<bool>>(_ => () => builder.Environment.IsDevelopment());
        var mapTileOptions = new MapTileOptions(
            builder.Configuration["MapTiles:TileUrlTemplate"] ?? MapTileOptions.Default.TileUrlTemplate,
            builder.Configuration["MapTiles:Attribution"] ?? MapTileOptions.Default.Attribution);

        builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri(apiUrl) });
        builder.Services.AddSingleton<AuthClient>();

        // Everything the desktop app needs to reach the tenant database. No Master connection string.
        builder.Services.AddWayPlotTenantClient(serverName);

        // The shell is built after login, from the signed-in user's LoginResult.
        builder.Services.AddSingleton<Func<LoginResult, MainShell>>(sp => login =>
        {
            var presenter = new MainShellPresenter(login, sp.GetRequiredService<ITenantFeatures>());
            var views = new Dictionary<string, Func<Control>>
            {
                ["parcel-map"] = () =>
                {
                    var view = new ParcelMapView();
                    view.ConfigureMapTiles(mapTileOptions);
                    view.Presenter = new ParcelMapPresenter(
                        view, sp.GetRequiredService<ParcelService>(), sp.GetRequiredService<LeadService>(),
                        sp.GetRequiredService<ContactService>(), sp.GetRequiredService<InteractionService>(),
                        sp.GetRequiredService<ParcelPhotoService>(),
                        sp.GetRequiredService<TenantAccessPolicy>());
                    return view;
                },
                ["contacts"] = () =>
                {
                    var view = new ContactsView();
                    view.Presenter = new ContactsPresenter(
                        view,
                        sp.GetRequiredService<ContactService>(),
                        sp.GetRequiredService<ContactImportService>(),
                        sp.GetRequiredService<LeadService>(),
                        sp.GetRequiredService<ParcelService>(),
                        sp.GetRequiredService<WayPlot.domain.Services.Admin.StaffService>(),
                        sp.GetRequiredService<DealService>(),
                        sp.GetRequiredService<ICurrentUser>(),
                        sp.GetRequiredService<TenantAccessPolicy>());
                    return view;
                },
                ["pipeline"] = () =>
                {
                    var view = new PipelineView();
                    view.Presenter = new PipelinePresenter(
                        view,
                        sp.GetRequiredService<LeadService>(),
                        sp.GetRequiredService<InteractionService>(),
                        sp.GetRequiredService<ParcelService>(),
                        sp.GetRequiredService<DealService>(),
                        sp.GetRequiredService<WayPlot.domain.Services.Admin.StaffService>(),
                        sp.GetRequiredService<IPipelineStageProvider>(),
                        sp.GetRequiredService<PipelineConfigService>(),
                        sp.GetRequiredService<ICurrentUser>(),
                        sp.GetRequiredService<TenantAccessPolicy>());
                    return view;
                },
                ["deals"] = () =>
                {
                    var view = new DealsView();
                    view.Presenter = new DealsPresenter(
                        view,
                        sp.GetRequiredService<DealService>(),
                        sp.GetRequiredService<ICurrentUser>(),
                        sp.GetRequiredService<IPipelineStageProvider>());
                    return view;
                },
                ["commissions"] = () =>
                {
                    var view = new CommissionsView();
                    view.Presenter = new CommissionsPresenter(
                        view,
                        sp.GetRequiredService<CommissionService>(),
                        sp.GetRequiredService<ICurrentUser>(),
                        sp.GetRequiredService<ITenantFeatures>(),
                        sp.GetRequiredService<WayPlot.domain.Services.Admin.CompanyProfileService>(),
                        sp.GetRequiredService<WayPlot.domain.Services.Admin.StaffService>(),
                        sp.GetRequiredService<ParcelService>());
                    return view;
                },
                ["team"] = () =>
                {
                    var view = new TeamAdminView();
                    view.Presenter = new TeamAdminPresenter(
                        sp.GetRequiredService<WayPlot.domain.Services.Admin.StaffService>(),
                        sp.GetRequiredService<LeadService>(),
                        sp.GetRequiredService<ContactService>(),
                        sp.GetRequiredService<ParcelService>(),
                        sp.GetRequiredService<WayPlot.domain.Services.Admin.CompanyProfileService>(),
                        sp.GetRequiredService<ITenantFeatures>().Branching,
                        sp.GetRequiredService<ICurrentUser>().UserId);
                    return view;
                },
                ["review"] = () =>
                {
                    var view = new AdminReviewView();
                    view.Presenter = new AdminReviewPresenter(
                        sp.GetRequiredService<DealService>(),
                        sp.GetRequiredService<CommissionService>(),
                        sp.GetRequiredService<IPipelineStageProvider>());
                    return view;
                }
            };
            return new MainShell(presenter, views);
        });

        builder.Services.AddTransient<LoginForm>();

        using var host = builder.Build();
        Application.Run(host.Services.GetRequiredService<LoginForm>());
    }
}
