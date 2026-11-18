using System.ComponentModel;
using System.Globalization;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.Presenters;
using WayPlot.UI;

namespace WayPlot.Views;

public partial class ContactsView : UserControl, IContactsView
{
    private ContactsPresenter? _presenter;
    private IReadOnlySet<PersonKind> _kinds = new HashSet<PersonKind>();
    private readonly HashSet<Guid> _selectedPeople = [];
    private Guid? _selectedPersonId;
    private PeopleListItemDto? _selectedPerson;
    private IReadOnlyList<PeopleListItemDto> _people = [];
    private int _dealLoadToken;
    private int _preferredDetailWidth = 380;
    private bool _settingsLoaded;
    private readonly System.Windows.Forms.Timer transientStatusTimer;
    private string _normalStatus = "Loading people...";

    public ContactsView()
    {
        InitializeComponent();
        transientStatusTimer = new System.Windows.Forms.Timer { Interval = 4000 };
        transientStatusTimer.Tick += (_, _) => RestoreStatus();
        ApplyTheme();

        txtSearch.TextChanged += (_, _) =>
        {
            searchTimer.Stop();
            searchTimer.Start();
        };
        searchTimer.Tick += async (_, _) =>
        {
            searchTimer.Stop();
            if (_presenter is not null)
                await _presenter.LoadAsync(txtSearch.Text);
        };

        btnKindAll.Click += async (_, _) => await SetKindAsync(null);
        btnKindCustomers.Click += async (_, _) => await SetKindAsync(PersonKind.Customer);
        btnKindClients.Click += async (_, _) => await SetKindAsync(PersonKind.Client);
        btnKindLandowners.Click += async (_, _) => await SetKindAsync(PersonKind.Landowner);
        cboAgent.SelectedIndexChanged += async (_, _) =>
        {
            if (_presenter is null) return;
            _presenter.SetAgentFilter(cboAgent.SelectedItem is StaffListItem staff && staff.Id != Guid.Empty
                ? staff.Id : null);
            await _presenter.LoadAsync(txtSearch.Text);
        };
        btnNew.Click += async (_, _) => await NewContactAsync();
        btnImport.Click += async (_, _) => await ImportCsvAsync();
        btnCloseDetail.Click += (_, _) => CloseDetail();
        listContacts.SelectedIndexChanged += async (_, _) => await SelectPersonAsync();
        listContacts.Resize += (_, _) => FitColumns();
        splitMain.SplitterMoved += (_, _) => SaveDetailWidth();

        splitMain.Panel2Collapsed = true;
        ApplyKindSelection(null);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<Guid>? ShowParcelOnMap { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Guid? ShowContactSelection { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ContactsPresenter? Presenter
    {
        set
        {
            _presenter = value;
            UpdateImportAction();
        }
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        LoadDetailWidth();
        UpdateImportAction();
        if (_presenter is not null && !DesignMode)
        {
            await LoadAgentsAsync();
            await _presenter.RefreshDealStatusesAsync();
            if (ShowContactSelection is { } contactId)
            {
                _kinds = new HashSet<PersonKind>();
                if (_presenter is not null)
                    _presenter.SetKindFilter(_kinds);
                ApplyKindSelection(null);
            }
            await _presenter!.LoadAsync();
            if (ShowContactSelection is { } pendingId)
            {
                var person = _people.FirstOrDefault(item => item.Id == pendingId);
                if (person is not null)
                {
                    foreach (ListViewItem item in listContacts.Items)
                    {
                        if (item.Tag is Guid id && id == pendingId)
                        {
                            item.Selected = true;
                            item.EnsureVisible();
                            break;
                        }
                    }
                }
                else
                {
                    var contact = await _presenter!.GetContactAsync(pendingId);
                    if (contact.Ok && contact.Value is not null)
                    {
                        txtSearch.Text = contact.Value.FullName;
                        await _presenter.LoadAsync(contact.Value.FullName);
                        foreach (ListViewItem item in listContacts.Items)
                        {
                            if (item.Tag is Guid id && id == pendingId)
                            {
                                item.Selected = true;
                                item.EnsureVisible();
                                break;
                            }
                        }
                    }
                }
                ShowContactSelection = null;
            }
        }
    }

    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        lblTitle.Font = Theme.Heading;
        lblTitle.ForeColor = Theme.Navy;
        lblSummary.Font = Theme.Body;
        lblSummary.ForeColor = Theme.Muted;
        txtSearch.Font = Theme.Body;
        txtSearch.PlaceholderText = "Search name, phone or email";
        listContacts.Font = Theme.Body;
        listContacts.BackColor = Theme.Surface;
        listContacts.ForeColor = Theme.Text;
        detailPanel.BackColor = Theme.Surface;
        detailBody.BackColor = Theme.Surface;
        ButtonStyle.Apply(btnNew, true);
        ButtonStyle.Apply(btnImport, false);
        ButtonStyle.Apply(btnCloseDetail, false);
        UpdateKindButtons();
    }

    private async Task SetKindAsync(PersonKind? kind)
    {
        _kinds = kind is null
            ? new HashSet<PersonKind>()
            : new HashSet<PersonKind> { kind.Value };
        if (_presenter is not null)
            _presenter.SetKindFilter(_kinds);
        ApplyKindSelection(kind);
        if (_presenter is not null)
            await _presenter.LoadAsync(txtSearch.Text);
    }

    private void ApplyKindSelection(PersonKind? kind)
    {
        UpdateKindButtons(kind);
    }

    private void UpdateKindButtons(PersonKind? kind = null)
    {
        foreach (var button in new[] { btnKindAll, btnKindCustomers, btnKindClients, btnKindLandowners })
            ButtonStyle.Apply(button, false);
        var selected = kind switch
        {
            PersonKind.Customer => btnKindCustomers,
            PersonKind.Client => btnKindClients,
            PersonKind.Landowner => btnKindLandowners,
            _ => btnKindAll
        };
        selected.BackColor = Theme.Navy;
        selected.ForeColor = Color.White;
    }

    private async Task LoadAgentsAsync()
    {
        if (_presenter is null || !_presenter.IsAdmin) return;
        var result = await _presenter.ListStaffAsync();
        if (!result.Ok || result.Value is null)
        {
            ShowStatus(result.Message, true);
            return;
        }
        cboAgent.Items.Clear();
        cboAgent.Items.Add(new StaffListItem(Guid.Empty, "", "All agents", "", null, true, 0, 0));
        foreach (var staff in result.Value.Where(item => item.Role == Roles.Agent && item.IsActive))
            cboAgent.Items.Add(staff);
        cboAgent.SelectedIndex = 0;
        cboAgent.Visible = true;
    }

    private void FitColumns()
    {
        colName.Width = LogicalToDeviceUnits(210);
        colKind.Width = LogicalToDeviceUnits(110);
        colPhone.Width = LogicalToDeviceUnits(140);
        colEmail.Width = LogicalToDeviceUnits(220);
        colAgent.Width = LogicalToDeviceUnits(140);
        colOpenLeads.Width = LogicalToDeviceUnits(90);
        colDeals.Width = LogicalToDeviceUnits(70);
        colBalance.Width = LogicalToDeviceUnits(120);
        colParcels.Width = LogicalToDeviceUnits(80);
        var used = colName.Width + colKind.Width + colPhone.Width + colEmail.Width
            + colAgent.Width + colOpenLeads.Width + colDeals.Width + colBalance.Width + colParcels.Width;
        colName.Width = Math.Max(colName.Width,
            listContacts.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - used + colName.Width);
    }

    private async Task NewContactAsync()
    {
        if (_presenter is null) return;
        var choices = await _presenter.GetReferralOptionsAsync();
        if (IsDisposed) return;
        if (!choices.Ok || choices.Value is null)
        {
            ShowStatus(choices.Message, true);
            return;
        }
        using var dialog = new ContactDialog
        {
            SaveHandler = _presenter.CaptureAsync,
            ReferralOptions = choices.Value
        };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK || dialog.Result is not { } saved)
            return;
        txtSearch.Clear();
        searchTimer.Stop();
        await _presenter.LoadAsync(null, saved.Message);
    }

    private async Task ImportCsvAsync()
    {
        if (_presenter is null || !_presenter.CanImport) return;
        using var dialog = new ContactImportDialog
        {
            ColumnsHandler = _presenter.ReadImportColumns,
            PreviewHandler = _presenter.PreviewImportAsync,
            ImportHandler = _presenter.ImportCsvAsync,
            CanMapAgent = _presenter.CanAssignImportedContacts
        };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK || dialog.Result is not { } summary)
            return;
        await _presenter.LoadAsync(txtSearch.Text,
            $"Import complete: {summary.ContactsCreated} contacts, {summary.LeadsCreated} leads, " +
            $"{summary.Duplicates} duplicates, {summary.Rejected} rejected.");
    }

    private void UpdateImportAction()
    {
        if (_presenter is null)
        {
            ButtonStyle.ApplyAvailability([
                new ButtonAvailability(btnNew, false, false, null, true),
                new ButtonAvailability(btnImport, false, false, null, false)
            ], actionToolTip);
            return;
        }
        var canCapture = _presenter.CanCapture;
        var canImport = _presenter.CanImport;
        ButtonStyle.ApplyAvailability([
            new ButtonAvailability(btnNew, canCapture, canCapture,
                canCapture ? null : _presenter.CaptureUnavailableReason, true),
            new ButtonAvailability(btnImport, canImport, canImport,
                canImport ? null : "Only Agents and Tenant Admins can import contacts.", false)
        ], actionToolTip);
    }

    public void ShowContacts(IReadOnlyList<PeopleListItemDto> people)
    {
        if (IsDisposed) return;
        _people = people;
        _normalStatus = people.Count == 200
            ? "Showing the first 200. Refine the search."
            : people.Count == 1 ? "1 person" : $"{people.Count} people";
        if (lblSummary.Text.StartsWith("Loading", StringComparison.Ordinal))
            lblSummary.Text = _normalStatus;
        listContacts.BeginUpdate();
        listContacts.Items.Clear();
        foreach (var person in people)
        {
            var kindText = person.Kind.ToString();
            var item = new ListViewItem(person.Name + (person.HasOverdueDeal ? "  OVERDUE" : "")) { Tag = person.Id };
            if (person.HasOverdueDeal)
            {
                item.ForeColor = Color.FromArgb(178, 49, 49);
            }
            item.SubItems.Add(kindText);
            item.SubItems.Add(Dash(person.Phone));
            item.SubItems.Add(Dash(person.Email));
            item.SubItems.Add(Dash(person.AssignedAgentName));
            item.SubItems.Add(person.OpenLeadCount.ToString(CultureInfo.InvariantCulture));
            item.SubItems.Add(person.DealCount.ToString(CultureInfo.InvariantCulture));
            item.SubItems.Add(person.Kind == PersonKind.Client
                ? Peso(person.BalanceTotal ?? 0)
                : "—");
            item.SubItems.Add(person.Kind == PersonKind.Landowner
                ? person.ParcelCount.ToString(CultureInfo.InvariantCulture)
                : "—");
            listContacts.Items.Add(item);
        }
        listContacts.EndUpdate();
        FitColumns();

        if (_selectedPersonId is { } selectedId)
        {
            foreach (ListViewItem item in listContacts.Items)
            {
                if (item.Tag is Guid id && id == selectedId)
                {
                    item.Selected = true;
                    item.EnsureVisible();
                    break;
                }
            }
        }
    }

    private async Task SelectPersonAsync()
    {
        if (_presenter is null || listContacts.SelectedItems.Count != 1)
            return;
        if (listContacts.SelectedItems[0].Tag is not Guid personId)
            return;

        var person = await FindPersonAsync(personId);
        if (person is null) return;
        _selectedPersonId = personId;
        _selectedPerson = person;
        await OpenDetailAsync(person);
    }

    private async Task<PeopleListItemDto?> FindPersonAsync(Guid id)
    {
        await Task.CompletedTask;
        return _people.FirstOrDefault(person => person.Id == id);
    }

    private async Task OpenDetailAsync(PeopleListItemDto person)
    {
        if (_presenter is null) return;
        lblDetailName.Text = person.Name;
        splitMain.Panel2Collapsed = false;
        ApplyPreferredDetailWidth();

        detailBody.Controls.Clear();
        detailBody.SuspendLayout();

        if (person.Kind is PersonKind.Customer or PersonKind.Client)
        {
            var contact = await _presenter.GetContactAsync(person.Id);
            if (contact.Ok && contact.Value is { } c)
            {
                AddDetailLine($"Kind: {person.Kind}");
                AddDetailLine($"Phone: {Dash(c.Phone)}");
                AddDetailLine($"Email: {Dash(c.Email)}");
                AddDetailLine($"Source: {EnumText.Pretty(c.Source)}");
                AddDetailLine($"SMS consent: {(c.ConsentSms ? "Yes" : "No")}");
                AddDetailLine($"Email consent: {(c.ConsentEmail ? "Yes" : "No")}");
                AddDetailLine($"Assigned agent: {Dash(person.AssignedAgentName)}");
            }

            await LoadDealsSectionAsync(person);
            AddDetailHeading("Leads");
            var leads = await _presenter.ListByContactAsync(person.Id);
            if (!leads.Ok || leads.Value is null)
            {
                AddDetailLine(leads.Message);
            }
            else
            {
                foreach (var lead in leads.Value)
                    AddLeadCard(person, lead);
            }
            AddMutationButton("Add lead", async () => await AddLeadAsync(person), true);
        }
        else
        {
            AddDetailLine($"Kind: Landowner");
            AddDetailLine($"Phone: {Dash(person.Phone)}");
            AddDetailLine($"Email: {Dash(person.Email)}");
            AddDetailHeading("Parcels");
            var parcels = await _presenter.ListParcelsAsync();
            if (parcels.Ok && parcels.Value is { } all)
            {
                foreach (var parcel in all.Where(item =>
                             person.OwnerId is { } ownerId && item.OwnerId == ownerId))
                    AddParcelRow(parcel);
            }
        }

        detailBody.ResumeLayout();
        detailScroll.VerticalScroll.Value = 0;
    }

    private async Task LoadDealsSectionAsync(PeopleListItemDto person)
    {
        if (_presenter is null) return;
        var token = ++_dealLoadToken;
        AddDetailHeading("Deals");
        var result = await _presenter.GetDealSummariesAsync(person.Id);
        if (token != _dealLoadToken || IsDisposed) return;
        if (!result.Ok || result.Value is null)
        {
            AddDetailLine(result.Message);
            return;
        }
        var deals = result.Value.ToList();
        var remaining = deals.Sum(deal => deal.Balance);
        AddDetailLine(deals.Count == 0
            ? "No deals yet."
            : $"{deals.Count} deals, {Peso(remaining)} remaining");
        foreach (var deal in deals)
        {
            var card = new DealCardControl
            {
                Deal = deal,
                Dock = DockStyle.Top,
                Margin = new Padding(12, 4, 12, 8),
                LoadPayments = async dealId =>
                {
                    var payments = await _presenter.GetPaymentsAsync(dealId);
                    return payments.Ok && payments.Value is not null
                        ? payments.Value
                        : Array.Empty<DealPaymentDto>();
                },
                ShowMapRequested = id => ShowParcelOnMap?.Invoke(id)
            };
            ConfigureDealCardActions(card);
            detailBody.Controls.Add(card);
        }
    }

    private void ConfigureDealCardActions(DealCardControl card)
    {
        if (_presenter is null) return;
        var deal = card.Deal;
        if (deal.IsPendingConfirmation)
        {
            if (_presenter.IsAdmin)
            {
                card.ConfirmRequested = async summary => await ConfirmDealAsync(summary);
                card.ReturnRequested = async summary => await ReturnDealRequestAsync(summary);
            }
            else if (_presenter.IsAgent)
            {
                card.CancelRequestRequested = async summary => await ReturnDealRequestAsync(summary);
            }
        }
        else if (_presenter.IsAdmin)
        {
            card.CancelRequested = async summary => await TerminateDealAsync(summary, false);
            card.DefaultRequested = async summary => await TerminateDealAsync(summary, true);
        }

        if (deal.Status is DealStatus.Current or DealStatus.Overdue)
        {
            if (_presenter.IsAgent || _presenter.IsAdmin)
                card.RecordPaymentRequested = async summary => await RecordPaymentAsync(summary);
        }
        if (_presenter.IsAdmin)
        {
            card.ConfirmPaymentRequested = async payment => await ConfirmPaymentAsync(payment);
            card.ReturnPaymentRequested = async payment => await ReturnPaymentAsync(payment);
        }
    }

    private async Task RecordPaymentAsync(DealSummaryDto summary)
    {
        if (_presenter is null) return;
        var dealItem = new DealListItem(
            summary.DealId, Guid.Empty, "", summary.ParcelLabel, LeadStage.Closed, true,
            summary.NetPrice, 0, 0, 0, 0, summary.Balance, summary.Status,
            !summary.IsPendingConfirmation, true, DateTime.UtcNow,
            summary.DealType == DealType.Lease ? ParcelStatus.Leased : ParcelStatus.Sold,
            true, false);
        using var dialog = new RecordPaymentDialog
        {
            Deal = dealItem,
            PrefillAmount = summary.OverdueAmount > 0
                ? summary.OverdueAmount
                : summary.NextDueAmount,
            SaveHandler = _presenter.RecordPaymentAsync
        };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
        ShowStatus("Payment recorded.", false);
        await RefreshSelectedDealSectionAsync();
        await RefreshListAsync();
        await _presenter.RefreshDealStatusesAsync();
    }

    private async Task ConfirmDealAsync(DealSummaryDto summary)
    {
        if (_presenter is null) return;
        using var dialog = new ConfirmClosedDialog();
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
        var result = await _presenter.ConfirmClosedAsync(summary.DealId, dialog.SelectedStatus);
        ShowStatus(result.Ok ? result.Message : result.Message, !result.Ok);
        await RefreshSelectedDealSectionAsync();
        await RefreshListAsync();
        await _presenter.RefreshDealStatusesAsync();
    }

    private async Task ReturnDealRequestAsync(DealSummaryDto summary)
    {
        if (_presenter is null) return;
        if (MessageBox.Show(FindForm(), "Return this pending Closed request?",
                "Return request", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        var result = await _presenter.ReturnRequestAsync(summary.DealId);
        ShowStatus(result.Message, !result.Ok);
        await RefreshSelectedDealSectionAsync();
        await RefreshListAsync();
    }

    private async Task TerminateDealAsync(DealSummaryDto summary, bool defaulted)
    {
        if (_presenter is null) return;
        var summaryResult = await _presenter.GetTerminationSummaryAsync(summary.DealId);
        if (!summaryResult.Ok || summaryResult.Value is null)
        {
            ShowStatus(summaryResult.Message, true);
            return;
        }
        using var dialog = new TerminateDealDialog(defaulted, summaryResult.Value);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK || dialog.Result is not { } result)
            return;
        var save = defaulted
            ? await _presenter.DefaultDealAsync(summary.DealId, result.Reason, result.MoneyOutcome, result.RefundedAmount)
            : await _presenter.CancelDealAsync(summary.DealId, result.Reason, result.MoneyOutcome, result.RefundedAmount);
        ShowStatus(save.Message, !save.Ok);
        await RefreshSelectedDealSectionAsync();
        await RefreshListAsync();
    }

    private async Task ConfirmPaymentAsync(DealPaymentDto payment)
    {
        if (_presenter is null) return;
        var result = await _presenter.ConfirmPaymentAsync(payment.Id);
        ShowStatus(result.Message, !result.Ok);
        await RefreshSelectedDealSectionAsync();
        await RefreshListAsync();
        await _presenter.RefreshDealStatusesAsync();
    }

    private async Task ReturnPaymentAsync(DealPaymentDto payment)
    {
        if (_presenter is null) return;
        using var dialog = new ReturnPaymentDialog
        {
            SaveHandler = async note => await _presenter.ReturnPaymentAsync(payment.Id, note)
        };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
        var result = dialog.Result;
        if (result is not { } returnResult) return;
        ShowStatus(returnResult.Message, !returnResult.Ok);
        await RefreshSelectedDealSectionAsync();
        await RefreshListAsync();
    }

    private async Task RefreshSelectedDealSectionAsync()
    {
        if (_selectedPerson is null) return;
        await OpenDetailAsync(_selectedPerson);
    }

    private async Task RefreshListAsync()
    {
        if (_presenter is null) return;
        await _presenter.LoadAsync(txtSearch.Text);
    }

    private void AddDetailHeading(string text)
    {
        var label = new Label
        {
            Text = text,
            AutoSize = true,
            Font = Theme.BodyBold,
            ForeColor = Theme.Navy,
            Margin = new Padding(12, 12, 12, 4)
        };
        detailBody.Controls.Add(label);
    }

    private void AddDetailLine(string text)
    {
        var label = new Label
        {
            Text = text,
            AutoSize = true,
            MaximumSize = new Size(300, 0),
            ForeColor = Theme.Text,
            Margin = new Padding(12, 4, 12, 4)
        };
        detailBody.Controls.Add(label);
    }

    private void AddLeadCard(PeopleListItemDto person, ContactLeadCardDto lead)
    {
        var card = new Panel
        {
            Margin = new Padding(12, 4, 12, 8),
            Padding = new Padding(10),
            BackColor = Theme.Page,
            Width = 320,
            Height = 122
        };
        var title = new Label
        {
            Text = lead.ParcelLabel,
            Font = Theme.BodyBold,
            ForeColor = Theme.Navy,
            Location = new Point(10, 8),
            AutoSize = true
        };
        var stage = new Label
        {
            Text = $"Stage: {lead.Stage}",
            Location = new Point(10, 34),
            AutoSize = true,
            ForeColor = Theme.Text
        };
        var agent = new Label
        {
            Text = $"Agent: {lead.AgentName}",
            Location = new Point(10, 54),
            AutoSize = true,
            ForeColor = Theme.Muted
        };
        var map = new Button { Text = "Show on map", Location = new Point(10, 82), AutoSize = true };
        ButtonStyle.Apply(map, false);
        map.Click += (_, _) => ShowParcelOnMap?.Invoke(lead.ParcelId);
        card.Controls.AddRange([title, stage, agent, map]);

        if (_presenter is not null && _presenter.CanAddOrSwitchLeads
            && lead.Stage is not LeadStage.Closed and not LeadStage.Lost)
        {
            var switchButton = new Button
            {
                Text = "Switch lot",
                Location = new Point(125, 82),
                AutoSize = true
            };
            ButtonStyle.Apply(switchButton, false);
            switchButton.Click += async (_, _) => await SwitchLotAsync(person, lead);
            card.Controls.Add(switchButton);

            var requestClosed = new Button
            {
                Text = "Request Closed",
                Location = new Point(10, 108),
                AutoSize = true
            };
            card.Height = 148;
            ButtonStyle.Apply(requestClosed, false);
            _ = ConfigureRequestClosedButtonAsync(requestClosed, lead.LeadId);
            card.Controls.Add(requestClosed);
        }
        detailBody.Controls.Add(card);
    }

    private async Task ConfigureRequestClosedButtonAsync(Button button, Guid leadId)
    {
        if (_presenter is null) return;
        var result = await _presenter.GetAllowedMovesAsync(leadId);
        var option = result.Ok && result.Value is not null
            ? result.Value.FirstOrDefault(move => move.OpensClosedRequest)
            : null;
        if (IsDisposed) return;
        if (option is null)
        {
            button.Enabled = false;
            actionToolTip.SetToolTip(button, "Closed request options are unavailable.");
            return;
        }
        button.Enabled = option.Allowed;
        actionToolTip.SetToolTip(button, option.Allowed ? "Request Closed" : option.Reason);
        if (option.Allowed)
        {
            button.Click += async (_, _) => await RequestClosedAsync(leadId);
        }
    }

    private async Task RequestClosedAsync(Guid leadId)
    {
        if (_presenter is null) return;
        var candidates = await _presenter.GetDealCandidatesAsync();
        if (!candidates.Ok || candidates.Value is null)
        {
            ShowStatus(candidates.Message, true);
            return;
        }
        var candidate = candidates.Value.FirstOrDefault(item => item.LeadId == leadId);
        if (candidate is null)
        {
            ShowStatus("This lead is not eligible for a Closed request.", true);
            return;
        }
        using var dialog = new CreateDealDialog
        {
            Candidates = [candidate],
            SaveHandler = _presenter.RequestClosedAsync
        };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
        ShowStatus("Closed request submitted.", false);
        await RefreshSelectedDealSectionAsync();
        await RefreshListAsync();
    }

    private void AddParcelRow(ParcelDto parcel)
    {
        var row = new TableLayoutPanel
        {
            ColumnCount = 2,
            AutoSize = true,
            Margin = new Padding(12, 4, 12, 4)
        };
        row.Controls.Add(new Label
        {
            Text = $"{parcel.Label} — {EnumText.Pretty(parcel.Status)}",
            AutoSize = true,
            Margin = new Padding(0, 4, 8, 4)
        }, 0, 0);
        var map = new Button { Text = "Show on map", AutoSize = true };
        ButtonStyle.Apply(map, false);
        map.Click += (_, _) => ShowParcelOnMap?.Invoke(parcel.Id);
        row.Controls.Add(map, 1, 0);
        detailBody.Controls.Add(row);
    }

    private void AddMutationButton(string text, Func<Task> action, bool primary)
    {
        if (_presenter is null) return;
        var button = new Button { Text = text, AutoSize = true, Margin = new Padding(12, 8, 12, 12) };
        if (_presenter.CanAddOrSwitchLeads)
        {
            ButtonStyle.Apply(button, primary);
            button.Click += async (_, _) => await action();
        }
        else
        {
            ButtonStyle.ApplyAvailability([
                new ButtonAvailability(button, false, false, _presenter.LeadMutationUnavailableReason, primary)
            ], actionToolTip);
        }
        detailBody.Controls.Add(button);
    }

    private async Task AddLeadAsync(PeopleListItemDto person)
    {
        if (_presenter is null) return;
        var parcels = await _presenter.ListParcelsAsync();
        if (!parcels.Ok || parcels.Value is null)
        {
            ShowStatus(parcels.Message, true);
            return;
        }
        using var dialog = new AddLeadDialog();
        dialog.SetParcels(parcels.Value);
        if (_presenter.IsAdmin && person.AssignedAgentName is null)
        {
            var staff = await _presenter.ListStaffAsync();
            if (!staff.Ok || staff.Value is null)
            {
                ShowStatus(staff.Message, true);
                return;
            }
            dialog.ShowAgentPicker = true;
            dialog.SetAgents(staff.Value);
        }
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK || dialog.SelectedParcel is not { } parcel)
            return;
        var result = await _presenter.AddLeadAsync(person.Id, parcel.Id, dialog.Note, dialog.SelectedAgentId);
        if (!result.Ok)
        {
            ShowStatus(result.Message, true);
            return;
        }
        ShowStatus(result.Message, false);
        await OpenDetailAsync(person);
    }

    private async Task SwitchLotAsync(PeopleListItemDto person, ContactLeadCardDto lead)
    {
        if (_presenter is null) return;
        var parcels = await _presenter.ListParcelsAsync();
        if (!parcels.Ok || parcels.Value is null)
        {
            ShowStatus(parcels.Message, true);
            return;
        }
        using var dialog = new SwitchLotDialog(lead.ParcelId, lead.ParcelLabel);
        dialog.SetParcels(parcels.Value);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK || dialog.SelectedParcel is not { } parcel)
            return;
        var result = await _presenter.SwitchLotAsync(lead.LeadId, parcel.Id, dialog.Reason);
        if (!result.Ok)
        {
            ShowStatus(result.Message, true);
            return;
        }
        ShowStatus(result.Message, false);
        await _presenter.LoadAsync(txtSearch.Text);
        await OpenDetailAsync(person);
    }

    private void CloseDetail()
    {
        splitMain.Panel2Collapsed = true;
        _selectedPersonId = null;
        _selectedPerson = null;
        detailBody.Controls.Clear();
        if (listContacts.SelectedItems.Count > 0)
            listContacts.SelectedItems[0].Selected = false;
    }

    private void ApplyPreferredDetailWidth()
    {
        if (splitMain.Panel2Collapsed) return;
        var maxWidth = splitMain.ClientSize.Width - splitMain.Panel1MinSize - splitMain.SplitterWidth;
        if (maxWidth < splitMain.Panel2MinSize) return;
        var width = Math.Clamp(_preferredDetailWidth, splitMain.Panel2MinSize, maxWidth);
        splitMain.SplitterDistance = splitMain.ClientSize.Width - width - splitMain.SplitterWidth;
    }

    private static readonly string InspectorWidthPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WayPlot",
        "parcel-map.settings");

    private void LoadDetailWidth()
    {
        _settingsLoaded = true;
        try
        {
            if (File.Exists(InspectorWidthPath)
                && int.TryParse(File.ReadAllText(InspectorWidthPath), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var width)
                && width >= splitMain.Panel2MinSize)
                _preferredDetailWidth = width;
        }
        catch
        {
            // The default panel width remains usable if settings cannot be read.
        }
    }

    private void SaveDetailWidth()
    {
        if (!_settingsLoaded || splitMain.Panel2Collapsed || IsDisposed) return;
        var width = splitMain.ClientSize.Width - splitMain.SplitterDistance - splitMain.SplitterWidth;
        if (width < splitMain.Panel2MinSize) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(InspectorWidthPath)!);
            File.WriteAllText(InspectorWidthPath, width.ToString(CultureInfo.InvariantCulture));
        }
        catch
        {
            // Width persistence is best-effort.
        }
    }

    public void ShowStatus(string text, bool isError)
    {
        if (IsDisposed) return;
        lblSummary.Text = text;
        lblSummary.ForeColor = isError
            ? ColorTranslator.FromHtml("#8F2A1B")
            : Theme.Muted;
        transientStatusTimer.Stop();
        transientStatusTimer.Start();
    }

    private void RestoreStatus()
    {
        transientStatusTimer.Stop();
        lblSummary.Text = _normalStatus;
        lblSummary.ForeColor = Theme.Muted;
    }

    private static string Dash(string? text) =>
        string.IsNullOrWhiteSpace(text) ? "—" : text;

    private static string Peso(decimal amount) =>
        "₱" + amount.ToString("N0", CultureInfo.InvariantCulture);
}
