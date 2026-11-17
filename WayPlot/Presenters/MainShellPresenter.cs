using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Interfaces;

namespace WayPlot.Presenters;

public sealed record ShellTab(string Key, string Title, bool Enabled, string? Hint = null);

/// <summary>Decides which tabs the shell shows and which are enabled for the signed-in tenant's plan. The view only draws them.</summary>
public sealed class MainShellPresenter(LoginResult login, ITenantFeatures features)
{
    public string AppName => "WayPlot";
    public string Workspace => login.TenantName ?? "Platform";
    public string UserLine => $"{login.DisplayName} ({login.Role}) · Tier {login.Tier?.ToString() ?? "-"}";

    public IReadOnlyList<ShellTab> BuildTabs()
    {
        var tabs = new List<ShellTab>
        {
            Tab("parcel-map", "Parcel map", features.DataCollection),
            Tab("contacts", "Contacts", features.DataCollection),
            Tab("leads", "Leads", features.MainTransaction),
            Tab("interactions", "Interactions", features.MainTransaction),
            Tab("deals", "Deals & payments", features.DealsPayments)
        };
        if (login.Role != Roles.SuperAdmin)
            tabs.Add(Tab("commissions", "Commissions", features.DealsPayments));
        tabs.Add(Tab("import", "Import", features.DataCollection));
        tabs.Add(Tab("intelligence", "Intelligence", features.BusinessIntelligence, "Available in Tier B"));
        return tabs;
    }

    private static ShellTab Tab(string key, string title, bool enabled, string? disabledHint = null) =>
        new(key, title, enabled, enabled ? null : disabledHint ?? "Not included in your plan");
}