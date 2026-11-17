using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WayPlot.domain.Contracts;
using WayPlot.domain.Interfaces;
using WayPlot.infrastructure;
using WayPlot.Presenters;
using WayPlot.Views;
using WayPlot.domain.Services.DataCollection;

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
                    view.Presenter = new ParcelMapPresenter(
                        view, sp.GetRequiredService<ParcelService>(), sp.GetRequiredService<LeadService>(),
                        sp.GetRequiredService<ContactService>());
                    return view;
                },
                ["contacts"] = () =>
                {
                    var view = new ContactsView();
                    view.Presenter = new ContactsPresenter(view, sp.GetRequiredService<ContactService>());
                    return view;
                },
                ["leads"] = () =>
                {
                    var view = new LeadsView();
                    view.Presenter = new LeadsPresenter(view, sp.GetRequiredService<LeadService>());
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