using System.ComponentModel;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.UI;

namespace WayPlot.Views;

public partial class LogInteractionDialog : Form
{
    private readonly InteractionChannel[] _channels = Enum.GetValues<InteractionChannel>();
    private readonly List<Guid> _leadIds = [];
    private int _searchTicket;

    public LogInteractionDialog()
    {
        InitializeComponent();
        ApplyTheme();

        foreach (var kind in InteractionLogKinds.All)
            cboType.Items.Add(kind);
        foreach (var channel in _channels)
            cboChannel.Items.Add(EnumText.Pretty(channel));
        cboRating.Items.Add("No rating");
        for (var rating = 1; rating <= 5; rating++)
            cboRating.Items.Add(rating.ToString());
        cboType.SelectedIndex = 0;
        cboRating.SelectedIndex = 0;
        dtpOccurredAt.Value = DateTime.Now;
        UpdateInteractionFields();

        cboType.SelectedIndexChanged += (_, _) => UpdateInteractionFields();
        txtLeadSearch.TextChanged += (_, _) => { searchTimer.Stop(); searchTimer.Start(); };
        searchTimer.Tick += async (_, _) =>
        {
            searchTimer.Stop();
            await LoadLeadsAsync();
        };
        btnSave.Click += async (_, _) => await SaveAsync();
        Shown += async (_, _) => await InitializeLeadAsync();
        FormClosed += (_, _) => searchTimer.Stop();
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<string?, Task<ServiceResult<IReadOnlyList<LeadListItem>>>>? LeadSearchHandler { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<LogInteractionRequest, Task<ServiceResult<InteractionListItem>>>? SaveHandler { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Guid? InitialLeadId { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? InitialLeadText { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool LeadLocked { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ServiceResult<InteractionListItem>? Result { get; private set; }

    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        Font = Theme.Body;
        lblTitle.Font = Theme.Heading;
        lblTitle.ForeColor = Theme.Navy;
        foreach (var label in new[] { lblLead, lblType, lblChannel, lblWhen, lblNote, lblRating })
        {
            label.Font = Theme.Small;
            label.ForeColor = Theme.Muted;
        }
        lblLeadStatus.ForeColor = Theme.Muted;
        lblError.ForeColor = ColorTranslator.FromHtml("#8F2A1B");
        txtLeadSearch.Font = Theme.Body;
        cboLead.Font = Theme.Body;
        cboType.Font = Theme.Body;
        cboChannel.Font = Theme.Body;
        dtpOccurredAt.Font = Theme.Body;
        txtNote.Font = Theme.Body;
        cboRating.Font = Theme.Body;
        ButtonStyle.Apply(btnSave, primary: true);
        ButtonStyle.Apply(btnCancel, primary: false);
    }

    private async Task InitializeLeadAsync()
    {
        if (LeadLocked)
        {
            if (InitialLeadId is not { } leadId)
            {
                ShowLeadStatus("The selected lead could not be loaded.", true);
                return;
            }

            _leadIds.Clear();
            _leadIds.Add(leadId);
            cboLead.Items.Clear();
            cboLead.Items.Add(InitialLeadText ?? leadId.ToString());
            cboLead.SelectedIndex = 0;
            cboLead.Enabled = false;
            txtLeadSearch.Enabled = false;
            return;
        }

        await LoadLeadsAsync();
    }

    private async Task LoadLeadsAsync()
    {
        var ticket = ++_searchTicket;
        if (LeadLocked) return;
        if (LeadSearchHandler is null)
        {
            ShowLeadStatus("Lead search is unavailable.", true);
            return;
        }

        ShowLeadStatus("Loading leads...", false);
        var result = await LeadSearchHandler(txtLeadSearch.Text);
        if (IsDisposed || ticket != _searchTicket) return;
        if (!result.Ok || result.Value is null)
        {
            ShowLeadStatus(result.Message, true);
            return;
        }

        _leadIds.Clear();
        cboLead.BeginUpdate();
        cboLead.Items.Clear();
        foreach (var lead in result.Value)
        {
            _leadIds.Add(lead.Id);
            cboLead.Items.Add($"{lead.ContactName} — {lead.ParcelLabel} ({EnumText.Pretty(lead.Stage)})");
        }
        cboLead.EndUpdate();
        ShowLeadStatus(result.Value.Count == 0 ? "No leads found." : $"{result.Value.Count} leads found.", false);
    }

    private void UpdateInteractionFields()
    {
        if (cboType.SelectedItem is not string kind) return;

        var requiredChannel = kind switch
        {
            InteractionLogKinds.Call => InteractionChannel.Call,
            InteractionLogKinds.Text => InteractionChannel.Sms,
            InteractionLogKinds.Viewing => InteractionChannel.Viewing,
            InteractionLogKinds.Note => InteractionChannel.Note,
            _ => (InteractionChannel?)null
        };
        if (requiredChannel is { } channel)
        {
            cboChannel.SelectedIndex = Array.IndexOf(_channels, channel);
            cboChannel.Enabled = false;
        }
        else
        {
            cboChannel.Enabled = true;
            if (cboChannel.SelectedIndex < 0)
                cboChannel.SelectedIndex = Array.IndexOf(_channels, InteractionChannel.Note);
        }

        var isFeedback = kind == InteractionLogKinds.Feedback;
        cboRating.Enabled = isFeedback;
        if (!isFeedback)
            cboRating.SelectedIndex = 0;
    }

    private async Task SaveAsync()
    {
        if (SaveHandler is null)
        {
            ShowError("Interaction logging is unavailable.");
            return;
        }
        if (cboLead.SelectedIndex < 0 || cboLead.SelectedIndex >= _leadIds.Count)
        {
            ShowError("Select a lead.");
            return;
        }
        if (cboType.SelectedItem is not string kind || cboChannel.SelectedIndex < 0)
        {
            ShowError("Choose an interaction type and channel.");
            return;
        }

        int? rating = kind == InteractionLogKinds.Feedback &&
                      cboRating.SelectedIndex > 0 &&
                      int.TryParse(cboRating.SelectedItem?.ToString(), out var parsedRating)
            ? parsedRating
            : null;
        var request = new LogInteractionRequest(
            _leadIds[cboLead.SelectedIndex],
            kind,
            _channels[cboChannel.SelectedIndex],
            dtpOccurredAt.Value.ToUniversalTime(),
            txtNote.Text,
            rating);

        lblError.Visible = false;
        btnSave.Enabled = false;
        try
        {
            var result = await SaveHandler(request);
            if (IsDisposed) return;
            if (!result.Ok)
            {
                ShowError(result.Message);
                return;
            }

            Result = result;
            DialogResult = DialogResult.OK;
        }
        finally
        {
            if (!IsDisposed) btnSave.Enabled = true;
        }
    }

    private void ShowLeadStatus(string text, bool isError)
    {
        if (IsDisposed) return;
        lblLeadStatus.Text = text;
        lblLeadStatus.ForeColor = isError ? ColorTranslator.FromHtml("#8F2A1B") : Theme.Muted;
    }

    private void ShowError(string text)
    {
        if (IsDisposed) return;
        lblError.Text = text;
        lblError.Visible = true;
    }

    private void ratingLayout_Paint(object sender, PaintEventArgs e)
    {

    }
}
