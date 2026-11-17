using System.ComponentModel;
using System.Globalization;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.Presenters;
using WayPlot.UI;

namespace WayPlot.Views;

public partial class LeadsView : UserControl, ILeadsView
{
    private LeadsPresenter? _presenter;

    public LeadsView()
    {
        InitializeComponent();
        ApplyTheme();

        cboStage.Items.Add("All stages");
        foreach (var stage in Enum.GetValues<LeadStage>())
            cboStage.Items.Add(EnumText.Pretty(stage));
        cboStage.SelectedIndex = 0;

        txtSearch.TextChanged += (_, _) => { searchTimer.Stop(); searchTimer.Start(); };
        searchTimer.Tick += async (_, _) =>
        {
            searchTimer.Stop();
            await LoadAsync();
        };
        cboStage.SelectedIndexChanged += async (_, _) =>
        {
            searchTimer.Stop();
            await LoadAsync();
        };
        btnOpenLeadFile.Click += async (_, _) => await OpenLeadFileAsync();
        listLeads.ItemActivate += async (_, _) => await OpenLeadFileAsync();
        listLeads.Resize += (_, _) => FitColumns();
        FitColumns();
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public LeadsPresenter? Presenter
    {
        set => _presenter = value;
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (_presenter is not null && !DesignMode)
            await LoadAsync();
    }

    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        lblTitle.Font = Theme.Heading;
        lblTitle.ForeColor = Theme.Navy;
        lblHelp.Font = Theme.Small;
        lblHelp.ForeColor = Theme.Muted;
        lblSummary.Font = Theme.Body;
        lblSummary.ForeColor = Theme.Muted;
        cboStage.Font = Theme.Body;
        txtSearch.Font = Theme.Body;
        txtSearch.PlaceholderText = "Search contact or parcel";
        listLeads.Font = Theme.Body;
        listLeads.BackColor = Theme.Surface;
        listLeads.ForeColor = Theme.Text;
        ButtonStyle.Apply(btnOpenLeadFile, primary: true);
    }

    private async Task LoadAsync()
    {
        if (_presenter is null) return;
        await _presenter.LoadAsync(SelectedStage, txtSearch.Text);
    }

    private async Task OpenLeadFileAsync()
    {
        if (_presenter is null) return;
        if (listLeads.SelectedItems.Count == 0 || listLeads.SelectedItems[0].Tag is not Guid leadId)
        {
            ShowStatus("Select a lead to open its file.", true);
            return;
        }

        searchTimer.Stop();
        var result = await _presenter.GetLeadFileAsync(leadId);
        if (IsDisposed) return;
        if (!result.Ok || result.Value is null)
        {
            ShowStatus(result.Message, true);
            return;
        }

        using var dialog = new LeadFileDialog(result.Value)
        {
            MoveHandler = _presenter.MoveStageAsync,
            LoadHandler = _presenter.GetLeadFileAsync
        };
        dialog.ShowDialog(FindForm());

        if (!IsDisposed)
            await LoadAsync();
    }

    public void ShowLeads(IReadOnlyList<LeadListItem> leads)
    {
        if (IsDisposed) return;

        listLeads.BeginUpdate();
        listLeads.Items.Clear();
        foreach (var lead in leads)
        {
            var item = new ListViewItem(lead.ContactName) { Tag = lead.Id };
            item.SubItems.Add(lead.ParcelLabel);
            item.SubItems.Add(EnumText.Pretty(lead.Stage));
            item.SubItems.Add(lead.OfferAmount is { } offer ? Peso(offer) : "—");
            item.SubItems.Add(lead.LastActivityAtUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture));
            listLeads.Items.Add(item);
        }
        listLeads.EndUpdate();
        FitColumns();
    }

    public void ShowStatus(string text, bool isError)
    {
        if (IsDisposed) return;
        lblSummary.Text = text;
        lblSummary.ForeColor = isError ? ColorTranslator.FromHtml("#8F2A1B") : Theme.Muted;
    }

    private LeadStage? SelectedStage =>
        cboStage.SelectedIndex <= 0 ? null : (LeadStage)(cboStage.SelectedIndex - 1);

    private void FitColumns()
    {
        colContact.Width = LogicalToDeviceUnits(210);
        colParcel.Width = LogicalToDeviceUnits(200);
        colStage.Width = LogicalToDeviceUnits(130);
        colOffer.Width = LogicalToDeviceUnits(140);
        var used = colContact.Width + colParcel.Width + colStage.Width + colOffer.Width;
        colLastActivity.Width = Math.Max(LogicalToDeviceUnits(170), listLeads.ClientSize.Width - used);
    }

    private static string Peso(decimal amount) => "₱" + amount.ToString("N0", CultureInfo.InvariantCulture);
}
