using System.ComponentModel;
using System.Globalization;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.Presenters;
using WayPlot.UI;

namespace WayPlot.Views;

public partial class InteractionsView : UserControl, IInteractionsView
{
    private InteractionsPresenter? _presenter;

    public InteractionsView()
    {
        InitializeComponent();
        ApplyTheme();

        cboType.Items.Add("All types");
        foreach (var type in Enum.GetValues<InteractionType>())
            cboType.Items.Add(EnumText.Pretty(type));
        cboType.SelectedIndex = 0;

        cboType.SelectedIndexChanged += async (_, _) => await LoadAsync();
        btnLogInteraction.Click += async (_, _) => await LogInteractionAsync();
        listInteractions.SelectedIndexChanged += (_, _) => ShowSelectedDetail();
        listInteractions.Resize += (_, _) => FitColumns();
        FitColumns();
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public InteractionsPresenter? Presenter
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
        lblSummary.Font = Theme.Body;
        lblSummary.ForeColor = Theme.Muted;
        cboType.Font = Theme.Body;
        listInteractions.Font = Theme.Body;
        listInteractions.BackColor = Theme.Surface;
        listInteractions.ForeColor = Theme.Text;
        txtDetail.Font = Theme.Body;
        txtDetail.BackColor = Theme.Surface;
        txtDetail.ForeColor = Theme.Text;
        ButtonStyle.Apply(btnLogInteraction, primary: true);
    }

    private async Task LoadAsync(string? notice = null)
    {
        if (_presenter is null) return;
        await _presenter.LoadAsync(SelectedType, notice);
    }

    private async Task LogInteractionAsync()
    {
        if (_presenter is null) return;

        using var dialog = new LogInteractionDialog
        {
            LeadSearchHandler = _presenter.SearchLeadsAsync,
            SaveHandler = _presenter.LogAsync
        };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK || dialog.Result is not { } saved)
            return;

        await LoadAsync(saved.Message);
    }

    public void ShowInteractions(IReadOnlyList<InteractionListItem> interactions)
    {
        if (IsDisposed) return;

        listInteractions.BeginUpdate();
        listInteractions.Items.Clear();
        foreach (var interaction in interactions)
        {
            var item = new ListViewItem(
                interaction.OccurredAtUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture))
            {
                Tag = interaction
            };
            item.SubItems.Add(interaction.ContactName);
            item.SubItems.Add(interaction.ParcelLabel);
            item.SubItems.Add(EnumText.Pretty(interaction.Type));
            item.SubItems.Add(EnumText.Pretty(interaction.Channel));
            item.SubItems.Add(ShortNote(interaction.Note));
            item.SubItems.Add(interaction.Rating?.ToString(CultureInfo.CurrentCulture) ?? "—");
            item.SubItems.Add(interaction.ComplaintStatus is { } status ? EnumText.Pretty(status) : "—");
            listInteractions.Items.Add(item);
        }
        listInteractions.EndUpdate();
        FitColumns();
        ShowSelectedDetail();
    }

    public void ShowStatus(string text, bool isError)
    {
        if (IsDisposed) return;
        lblSummary.Text = text;
        lblSummary.ForeColor = isError ? ColorTranslator.FromHtml("#8F2A1B") : Theme.Muted;
    }

    private void ShowSelectedDetail()
    {
        if (IsDisposed) return;
        txtDetail.Text = listInteractions.SelectedItems.Count > 0 &&
                         listInteractions.SelectedItems[0].Tag is InteractionListItem item
            ? item.Note
            : "Select an interaction to read its full note.";
    }

    private InteractionType? SelectedType =>
        cboType.SelectedIndex <= 0 ? null : (InteractionType)(cboType.SelectedIndex - 1);

    private void FitColumns()
    {
        colWhen.Width = LogicalToDeviceUnits(140);
        colContact.Width = LogicalToDeviceUnits(160);
        colParcel.Width = LogicalToDeviceUnits(160);
        colType.Width = LogicalToDeviceUnits(95);
        colChannel.Width = LogicalToDeviceUnits(90);
        colRating.Width = LogicalToDeviceUnits(70);
        colComplaintStatus.Width = LogicalToDeviceUnits(130);
        var used = colWhen.Width + colContact.Width + colParcel.Width + colType.Width +
                   colChannel.Width + colRating.Width + colComplaintStatus.Width;
        colNote.Width = Math.Max(LogicalToDeviceUnits(120), listInteractions.ClientSize.Width - used);
    }

    private static string ShortNote(string note)
    {
        const int maxLength = 56;
        return note.Length <= maxLength ? note : note[..(maxLength - 1)] + "…";
    }
}
