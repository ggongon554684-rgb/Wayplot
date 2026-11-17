using WayPlot.Presenters;
using WayPlot.UI;

namespace WayPlot.Views;

/// <summary>The main window. Layout is in MainShell.Designer.cs; the tab list comes from the presenter.</summary>
public partial class MainShell : Form
{
    private readonly IReadOnlyList<ShellTab> _tabs;

    /// <summary>Screens that exist. Steps 5 and 6 add "parcel-map" and "contacts" here. Any tab not listed shows Coming next.</summary>
    private readonly IReadOnlyDictionary<string, Func<Control>> _views;
    public MainShell(MainShellPresenter presenter, IReadOnlyDictionary<string, Func<Control>>? views = null)
    {
        _views = views ?? new Dictionary<string, Func<Control>>();
        InitializeComponent();
        WindowState = FormWindowState.Maximized;

        _tabs = presenter.BuildTabs();

        lblAppName.Text = presenter.AppName;
        lblWorkspace.Text = $"{presenter.Workspace}{Environment.NewLine}{presenter.UserLine}";
        ApplyTheme();

        tabBar.SelectedKeyChanged += (_, _) => ShowView(tabBar.SelectedKey);
        tabBar.SetTabs(
            _tabs.Select(t => new PillTabBar.Tab(t.Key, t.Title, t.Enabled, t.Hint)),
            _tabs.FirstOrDefault(t => t.Enabled)?.Key);
    }

    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        Font = Theme.Body;
        pnlContent.BackColor = Theme.Page;
        lblAppName.Font = Theme.Title;
        lblAppName.ForeColor = Theme.Navy;
        lblWorkspace.Font = Theme.Small;
        lblWorkspace.ForeColor = Theme.Muted;
    }

    private void ShowView(string? key)
    {
        if (key is null) return;

        foreach (var old in pnlContent.Controls.Cast<Control>().ToList())
        {
            pnlContent.Controls.Remove(old);
            old.Dispose();
        }

        var title = _tabs.FirstOrDefault(t => t.Key == key)?.Title ?? key;
        Control view = _views.TryGetValue(key, out var create) ? create() : new ComingNextView { Title = title };
        view.Dock = DockStyle.Fill;
        pnlContent.Controls.Add(view);
    }
}