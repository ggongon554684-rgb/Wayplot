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
        ButtonStyle.Apply(btnSignOut, primary: false);
        btnSignOut.Click += (_, _) => SignOut();
    }

    public event EventHandler? SignOutRequested;
    public bool IsSigningOut { get; private set; }

    private void SignOut()
    {
        if (MessageBox.Show(
                this,
                "Sign out of this account?",
                "Sign out",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        foreach (var view in pnlContent.Controls.Cast<Control>().ToList())
        {
            pnlContent.Controls.Remove(view);
            view.Dispose();
        }

        IsSigningOut = true;
        SignOutRequested?.Invoke(this, EventArgs.Empty);
        Close();
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
        if (view is ContactsView contactsView)
        {
            contactsView.ShowParcelOnMap = ShowParcelOnMap;
            contactsView.ShowContactSelection = _pendingContactId;
            _pendingContactId = null;
        }
        if (view is PipelineView pipelineView)
        {
            pipelineView.ShowParcelOnMap = ShowParcelOnMap;
            pipelineView.ShowContactRequested = ShowContact;
        }
        if (key == "parcel-map" && _pendingParcelId is { } parcelId
            && view is ParcelMapView mapView)
        {
            mapView.PendingParcelId = parcelId;
            _pendingParcelId = null;
        }
    }

    private Guid? _pendingParcelId;
    private Guid? _pendingContactId;

    public void ShowParcelOnMap(Guid parcelId)
    {
        _pendingParcelId = parcelId;
        tabBar.SelectKey("parcel-map");
    }

    public void ShowContact(Guid contactId)
    {
        _pendingContactId = contactId;
        tabBar.SelectKey("contacts");
    }
}