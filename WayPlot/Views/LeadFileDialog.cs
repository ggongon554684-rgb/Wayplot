using System.ComponentModel;
using System.Globalization;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.UI;

namespace WayPlot.Views;

public partial class LeadFileDialog : Form
{
    private LeadFileDto _file;

    public LeadFileDialog(LeadFileDto file)
    {
        _file = file;
        InitializeComponent();
        ApplyTheme();

        foreach (var stage in Enum.GetValues<LeadStage>())
            cboStage.Items.Add(EnumText.Pretty(stage));

        btnMoveStage.Click += async (_, _) => await MoveStageAsync();
        btnLogInteraction.Click += async (_, _) => await LogInteractionAsync();
        btnClose.Click += (_, _) => Close();
        ShowFile(_file);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<MoveStageRequest, Task<ServiceResult<LeadDto>>>? MoveHandler { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<Guid, Task<ServiceResult<LeadFileDto>>>? LoadHandler { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<string?, Task<ServiceResult<IReadOnlyList<LeadListItem>>>>? LeadSearchHandler { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<LogInteractionRequest, Task<ServiceResult<InteractionListItem>>>? InteractionSaveHandler { get; set; }

    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        Font = Theme.Body;
        lblContactName.Font = Theme.Heading;
        lblContactName.ForeColor = Theme.Navy;
        lblContactInfo.ForeColor = Theme.Muted;
        lblParcel.ForeColor = Theme.Text;
        lblStage.ForeColor = Theme.Text;
        lblOffer.ForeColor = Theme.Text;
        lblHistory.Font = Theme.BodyBold;
        lblHistory.ForeColor = Theme.Navy;
        lblInteractions.Font = Theme.BodyBold;
        lblInteractions.ForeColor = Theme.Navy;
        lblMove.ForeColor = Theme.Muted;
        lblStatus.ForeColor = Theme.Muted;
        cboStage.Font = Theme.Body;
        listHistory.Font = Theme.Body;
        listHistory.BackColor = Theme.Surface;
        listHistory.ForeColor = Theme.Text;
        listInteractions.Font = Theme.Body;
        listInteractions.BackColor = Theme.Surface;
        listInteractions.ForeColor = Theme.Text;
        ButtonStyle.Apply(btnMoveStage, primary: true);
        ButtonStyle.Apply(btnLogInteraction, primary: false);
        ButtonStyle.Apply(btnClose, primary: false);
    }

    private async Task MoveStageAsync()
    {
        if (MoveHandler is null) return;
        if (cboStage.SelectedIndex < 0)
        {
            ShowStatus("Select a stage.", true);
            return;
        }

        var request = new MoveStageRequest(_file.Id, (LeadStage)cboStage.SelectedIndex);
        btnMoveStage.Enabled = false;
        try
        {
            var result = await MoveHandler(request);
            if (IsDisposed) return;
            ShowStatus(result.Message, !result.Ok);
            if (!result.Ok || LoadHandler is null) return;

            var refreshed = await LoadHandler(_file.Id);
            if (IsDisposed) return;
            if (!refreshed.Ok || refreshed.Value is null)
            {
                ShowStatus(refreshed.Message, true);
                return;
            }

            _file = refreshed.Value;
            ShowFile(_file);
            ShowStatus(result.Message, false);
        }
        finally
        {
            if (!IsDisposed) btnMoveStage.Enabled = true;
        }
    }

    private async Task LogInteractionAsync()
    {
        if (LeadSearchHandler is null || InteractionSaveHandler is null || LoadHandler is null)
        {
            ShowStatus("Interaction logging is unavailable.", true);
            return;
        }

        using var dialog = new LogInteractionDialog
        {
            InitialLeadId = _file.Id,
            InitialLeadText = $"{_file.ContactName} — {_file.ParcelLabel}",
            LeadLocked = true,
            LeadSearchHandler = LeadSearchHandler,
            SaveHandler = InteractionSaveHandler
        };
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Result is not { } saved)
            return;

        var refreshed = await LoadHandler(_file.Id);
        if (IsDisposed) return;
        if (!refreshed.Ok || refreshed.Value is null)
        {
            ShowStatus(refreshed.Message, true);
            return;
        }

        _file = refreshed.Value;
        ShowFile(_file);
        ShowStatus(saved.Message, false);
    }

    private void ShowFile(LeadFileDto file)
    {
        if (IsDisposed) return;
        lblContactName.Text = file.ContactName;
        lblContactInfo.Text = $"Phone: {Dash(file.ContactPhone)} · Email: {Dash(file.ContactEmail)}";
        lblParcel.Text = "Parcel: " + file.ParcelLabel;
        lblStage.Text = "Stage: " + EnumText.Pretty(file.Stage);
        lblOffer.Text = file.OfferAmount is { } offer
            ? "Offer: ₱" + offer.ToString("N0", CultureInfo.CurrentCulture)
            : "Offer: —";

        listHistory.BeginUpdate();
        listHistory.Items.Clear();
        foreach (var row in file.History)
        {
            var from = row.FromStage is { } stage ? EnumText.Pretty(stage) : "Start";
            var item = new ListViewItem($"{from} → {EnumText.Pretty(row.ToStage)}");
            item.SubItems.Add(row.ChangedAtUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture));
            item.SubItems.Add(Dash(row.Note));
            listHistory.Items.Add(item);
        }
        listHistory.EndUpdate();

        listInteractions.BeginUpdate();
        listInteractions.Items.Clear();
        foreach (var row in file.Interactions)
        {
            var item = new ListViewItem(EnumText.Pretty(row.Type));
            item.SubItems.Add(EnumText.Pretty(row.Channel));
            item.SubItems.Add(row.OccurredAtUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture));
            item.SubItems.Add(Dash(row.Note));
            listInteractions.Items.Add(item);
        }
        listInteractions.EndUpdate();
    }

    private void ShowStatus(string text, bool isError)
    {
        if (IsDisposed) return;
        lblStatus.Text = text;
        lblStatus.ForeColor = isError ? ColorTranslator.FromHtml("#8F2A1B") : Theme.Muted;
    }

    private static string Dash(string? text) => string.IsNullOrWhiteSpace(text) ? "—" : text;
}
