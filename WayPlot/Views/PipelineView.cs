using System.ComponentModel;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.Presenters;
using WayPlot.UI;

namespace WayPlot.Views;

public partial class PipelineView : UserControl, IPipelineView
{
    private PipelinePresenter? _presenter;
    private Guid? _selectedLeadId;
    private PipelineBoardDto? _board;
    private readonly ToolTip _tooltip = new();
    private readonly System.Windows.Forms.Timer edgeScrollTimer;
    private IReadOnlyDictionary<string, LeadMoveOptionDto> _dragMoves =
        new Dictionary<string, LeadMoveOptionDto>();

    public PipelineView()
    {
        InitializeComponent();
        edgeScrollTimer = new System.Windows.Forms.Timer { Interval = 80 };
        edgeScrollTimer.Tick += (_, _) => ScrollBoardFromMouse();
        ApplyTheme();
        txtSearch.TextChanged += (_, _) => { filterTimer.Stop(); filterTimer.Start(); };
        filterTimer.Tick += async (_, _) => { filterTimer.Stop(); await ReloadAsync(); };
        cboParcel.SelectedIndexChanged += async (_, _) => await ReloadAsync();
        cboAgent.SelectedIndexChanged += async (_, _) => await ReloadAsync();
        chkOlder.CheckedChanged += async (_, _) => await ReloadAsync();
        splitDetail.SplitterMoved += (_, _) => SaveDetailWidth();
        btnMove.Click += async (_, _) => await OpenMoveMenuAsync();
        btnLogCall.Click += async (_, _) => await LogAsync(InteractionLogKinds.Call);
        btnLogText.Click += async (_, _) => await LogAsync(InteractionLogKinds.Text);
        btnLogEmail.Click += async (_, _) => await LogAsync(InteractionLogKinds.Email);
        btnLogViewing.Click += async (_, _) => await LogAsync(InteractionLogKinds.Viewing);
        btnAddNote.Click += async (_, _) => await LogAsync(InteractionLogKinds.Note);
        btnRequestClosed.Click += async (_, _) => await RequestClosedAsync();
        btnSwitchLot.Click += async (_, _) => await SwitchLotAsync();
        btnCloseDetail.Click += (_, _) => CloseDetail();
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public PipelinePresenter? Presenter
    {
        set
        {
            _presenter = value;
            UpdateRoleActions();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<Guid>? ShowParcelOnMap { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<Guid>? ShowContactRequested { get; set; }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        LoadDetailWidth();
        if (_presenter is not null && !DesignMode)
        {
            await LoadFiltersAsync();
            await ReloadAsync();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            edgeScrollTimer.Stop();
            edgeScrollTimer.Dispose();
            if (components != null) components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        txtSearch.Font = Theme.Body;
        txtSearch.PlaceholderText = "Search contact or parcel";
        cboParcel.Font = cboAgent.Font = Theme.Body;
        lblStatus.Font = Theme.Small;
        lblStatus.ForeColor = Theme.Muted;
        foreach (var button in new[] { btnMove, btnLogCall, btnLogText, btnLogEmail,
                                       btnLogViewing, btnAddNote, btnRequestClosed,
                                       btnSwitchLot, btnOpenContact, btnShowMap })
            ButtonStyle.Apply(button, false);
        ButtonStyle.Apply(btnCloseDetail, false);
    }

    private async Task LoadFiltersAsync()
    {
        if (_presenter is null) return;
        var parcels = await _presenter.ParcelsAsync();
        if (parcels.Ok && parcels.Value is not null)
        {
            cboParcel.Items.Clear();
            cboParcel.Items.Add(new ParcelFilter(Guid.Empty, "All parcels"));
            foreach (var parcel in parcels.Value)
                cboParcel.Items.Add(new ParcelFilter(parcel.Id, parcel.Label));
            cboParcel.DisplayMember = nameof(ParcelFilter.Name);
            cboParcel.SelectedIndex = 0;
        }
        if (_presenter.IsAdmin)
        {
            var staff = await _presenter.StaffAsync();
            if (staff.Ok && staff.Value is not null)
            {
                cboAgent.Items.Clear();
                cboAgent.Items.Add(new StaffFilter(Guid.Empty, "All agents"));
                foreach (var member in staff.Value.Where(item => item.Role == Roles.Agent))
                    cboAgent.Items.Add(new StaffFilter(member.Id, member.DisplayName));
                cboAgent.DisplayMember = nameof(StaffFilter.Name);
                cboAgent.SelectedIndex = 0;
                cboAgent.Visible = true;
            }
        }
    }

    private async Task ReloadAsync()
    {
        if (_presenter is null) return;
        var parcelId = cboParcel.SelectedItem is ParcelFilter parcel && parcel.Id != Guid.Empty
            ? parcel.Id : (Guid?)null;
        var agentId = cboAgent.Visible && cboAgent.SelectedItem is StaffFilter staff && staff.Id != Guid.Empty
            ? staff.Id : (Guid?)null;
        await _presenter.LoadAsync(new PipelineFilter(
            parcelId, agentId, txtSearch.Text, chkOlder.Checked));
    }

    public void ShowBoard(PipelineBoardDto board)
    {
        if (IsDisposed) return;
        _board = board;
        var selectedId = _selectedLeadId;
        boardPanel.SuspendLayout();
        boardPanel.Controls.Clear();
        foreach (var column in board.Columns)
        {
            var control = new PipelineColumnControl
            {
                Width = 280,
                Height = boardPanel.ClientSize.Height,
                Dock = DockStyle.Left,
                SharedToolTip = _tooltip
            };
            control.SetColumn(column, _presenter?.IsAdmin == true);
            control.CardSelected = SelectCard;
            if (_presenter?.CanMutate == true)
            {
                control.MoveRequested = async card => await OpenMoveMenuAsync(card.LeadId);
                control.DragStarting = async card =>
                {
                    await StartDragAsync(card.LeadId);
                    return _dragMoves.Values.ToList();
                };
                control.LeadDropped = async (leadId, stageKey) => await DropAsync(leadId, stageKey);
            }
            boardPanel.Controls.Add(control);
        }
        boardPanel.ResumeLayout();
        if (selectedId is { } id)
        {
            var selected = board.Columns.SelectMany(c => c.Cards).FirstOrDefault(c => c.LeadId == id);
            if (selected is not null)
                _ = SelectCardAsync(selected);
        }
    }

    public void ShowStatus(string text, bool isError)
    {
        if (IsDisposed) return;
        lblStatus.Text = text;
        lblStatus.ForeColor = isError ? ColorTranslator.FromHtml("#8F2A1B") : Theme.Muted;
    }

    private async Task StartDragAsync(Guid leadId)
    {
        if (_presenter is null) return;
        var result = await _presenter.GetAllowedMovesAsync(leadId);
        _dragMoves = result.Ok && result.Value is not null
            ? result.Value
                .Where(move => move.Allowed || move.Reason is not null)
                .ToDictionary(move => move.TargetStage.ToString(), move => move)
            : new Dictionary<string, LeadMoveOptionDto>();
        foreach (PipelineColumnControl column in boardPanel.Controls)
        {
            if (_dragMoves.TryGetValue(column.StageKey, out var move))
                column.SetDropState(move.Allowed, move.Reason);
            else
                column.SetDropState(false, "This target stage is unavailable.");
        }
        edgeScrollTimer.Start();
    }

    private async Task DropAsync(Guid leadId, string stageKey)
    {
        edgeScrollTimer.Stop();
        foreach (PipelineColumnControl column in boardPanel.Controls)
            column.ResetDropState();
        if (!Enum.TryParse<LeadStage>(stageKey, out var target))
            return;
        if (!_dragMoves.TryGetValue(stageKey, out var move) || !move.Allowed)
        {
            ShowStatus(move?.Reason ?? "This move is not allowed.", true);
            await ReloadAsync();
            return;
        }
        await ExecuteMoveAsync(leadId, target, move);
    }

    private async Task OpenMoveMenuAsync()
    {
        if (_selectedLeadId is not { } leadId) return;
        await OpenMoveMenuAsync(leadId);
    }

    private async Task OpenMoveMenuAsync(Guid leadId)
    {
        if (_presenter is null || !_presenter.CanMutate) return;
        var result = await _presenter.GetAllowedMovesAsync(leadId);
        if (!result.Ok || result.Value is null)
        {
            ShowStatus(result.Message, true);
            return;
        }
        var menu = new ContextMenuStrip();
        foreach (var move in result.Value.Where(move => move.Allowed))
        {
            var stageName = _presenter.Stages.FirstOrDefault(stage =>
                stage.Key == move.TargetStage.ToString())?.Name
                ?? move.TargetStage.ToString();
            menu.Items.Add(stageName, null, async (_, _) =>
                await ExecuteMoveAsync(leadId, move.TargetStage, move));
        }
        if (btnMove.IsHandleCreated)
            menu.Show(btnMove, new Point(0, btnMove.Height));
        else
            menu.Show(Cursor.Position);
    }

    private async Task ExecuteMoveAsync(Guid leadId, LeadStage target, LeadMoveOptionDto move)
    {
        if (_presenter is null) return;
        if (move.OpensClosedRequest)
        {
            await RequestClosedAsync(leadId);
            return;
        }
        using var dialog = new MoveStageDialog(_presenter.Stages);
        dialog.SelectStage(target);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK || dialog.Result is not { } result)
            return;
        var save = await _presenter.MoveAsync(new MoveStageRequest(
            leadId, result.TargetStage, result.Note, result.LostReason));
        if (!save.Ok)
        {
            ShowStatus(save.Message, true);
            await ReloadAsync();
            return;
        }
        ShowStatus(save.Message, false);
        await ReloadAsync();
    }

    private async Task RequestClosedAsync(Guid leadId)
    {
        if (_presenter is null) return;
        var candidates = await _presenter.DealCandidatesAsync();
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
        await ReloadAsync();
    }

    private async Task RequestClosedAsync() =>
        await RequestClosedAsync(_selectedLeadId ?? Guid.Empty);

    private async Task SwitchLotAsync()
    {
        if (_presenter is null || _selectedLeadId is not { } leadId) return;
        var card = _board?.Columns.SelectMany(column => column.Cards)
            .FirstOrDefault(item => item.LeadId == leadId);
        if (card is null) return;
        var parcels = await _presenter.ParcelsAsync();
        if (!parcels.Ok || parcels.Value is null)
        {
            ShowStatus(parcels.Message, true);
            return;
        }
        using var dialog = new SwitchLotDialog(card.ParcelId, card.ParcelLabel);
        dialog.SetParcels(parcels.Value);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK || dialog.SelectedParcel is not { } parcel)
            return;
        var result = await _presenter.SwitchLotAsync(leadId, parcel.Id, dialog.Reason);
        if (!result.Ok)
        {
            ShowStatus(result.Message, true);
            await ReloadAsync();
            return;
        }
        ShowStatus(result.Message, false);
        await ReloadAsync();
    }

    private async Task LogAsync(string kind)
    {
        if (_presenter is null || _selectedLeadId is not { } leadId) return;
        var card = _board?.Columns.SelectMany(column => column.Cards)
            .FirstOrDefault(item => item.LeadId == leadId);
        using var dialog = new LogInteractionDialog
        {
            InitialLeadId = leadId,
            InitialLeadText = card is null ? leadId.ToString() : $"{card.ContactName} — {card.ParcelLabel}",
            LeadLocked = true,
            LeadSearchHandler = _presenter.SearchLeadsAsync,
            SaveHandler = _presenter.LogAsync
        };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
        await ReloadAsync();
        await SelectCardAsync(card);
    }

    private async void SelectCard(LeadCardDto card) =>
        await SelectCardAsync(card);

    private async Task SelectCardAsync(LeadCardDto? card)
    {
        if (_presenter is null || card is null) return;
        _selectedLeadId = card.LeadId;
        splitDetail.Panel2Collapsed = false;
        ApplyPreferredDetailWidth();
        lblContact.Text = card.ContactName;
        lblParcel.Text = card.ParcelLabel;
        lblStage.Text = card.Stage.ToString();
        lblAgent.Text = card.AgentName ?? "Unassigned";
        var timeline = await _presenter.TimelineAsync(card.LeadId);
        timelineList.Controls.Clear();
        if (!timeline.Ok || timeline.Value is null)
        {
            timelineList.Controls.Add(new Label { Text = timeline.Message, AutoSize = true });
        }
        else
        {
            foreach (var item in timeline.Value)
            {
                timelineList.Controls.Add(new Label
                {
                    Text = $"{item.When.ToLocalTime():g} — {item.Title}" +
                           (string.IsNullOrWhiteSpace(item.Detail) ? "" : $" — {item.Detail}") +
                           (string.IsNullOrWhiteSpace(item.ByStaffName) ? "" : $" ({item.ByStaffName})"),
                    AutoSize = true,
                    MaximumSize = new Size(360, 0),
                    Margin = new Padding(8)
                });
            }
        }
        UpdateRoleActions();
    }

    private void CloseDetail()
    {
        _selectedLeadId = null;
        splitDetail.Panel2Collapsed = true;
    }

    private void UpdateRoleActions()
    {
        if (_presenter is null) return;
        var canMutate = _presenter.CanMutate;
        ButtonStyle.ApplyAvailability([
            new ButtonAvailability(btnMove, canMutate, canMutate, _presenter.MutationUnavailableReason, false),
            new ButtonAvailability(btnLogCall, canMutate, canMutate, _presenter.MutationUnavailableReason, false),
            new ButtonAvailability(btnLogText, canMutate, canMutate, _presenter.MutationUnavailableReason, false),
            new ButtonAvailability(btnLogEmail, canMutate, canMutate, _presenter.MutationUnavailableReason, false),
            new ButtonAvailability(btnLogViewing, canMutate, canMutate, _presenter.MutationUnavailableReason, false),
            new ButtonAvailability(btnAddNote, canMutate, canMutate, _presenter.MutationUnavailableReason, false),
            new ButtonAvailability(btnRequestClosed, canMutate, canMutate, _presenter.MutationUnavailableReason, false),
            new ButtonAvailability(btnSwitchLot, canMutate, canMutate, _presenter.MutationUnavailableReason, false)
        ], _tooltip);
        btnOpenContact.Visible = true;
        btnShowMap.Visible = true;
    }

    private void ScrollBoardFromMouse()
    {
        var point = boardPanel.PointToClient(Cursor.Position);
        if (point.X < 48)
            boardPanel.HorizontalScroll.Value = Math.Max(0, boardPanel.HorizontalScroll.Value - 24);
        else if (point.X > boardPanel.Width - 48)
            boardPanel.HorizontalScroll.Value = Math.Min(boardPanel.HorizontalScroll.Maximum,
                boardPanel.HorizontalScroll.Value + 24);
    }

    private static readonly string InspectorWidthPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WayPlot", "parcel-map.settings");

    private void LoadDetailWidth()
    {
        try
        {
            if (File.Exists(InspectorWidthPath)
                && int.TryParse(File.ReadAllText(InspectorWidthPath), out var width)
                && width >= splitDetail.Panel2MinSize)
                _preferredDetailWidth = width;
        }
        catch { }
    }

    private int _preferredDetailWidth = 380;

    private void ApplyPreferredDetailWidth()
    {
        var max = splitDetail.ClientSize.Width - splitDetail.Panel1MinSize - splitDetail.SplitterWidth;
        if (max < splitDetail.Panel2MinSize) return;
        splitDetail.SplitterDistance = splitDetail.ClientSize.Width
            - Math.Clamp(_preferredDetailWidth, splitDetail.Panel2MinSize, max)
            - splitDetail.SplitterWidth;
    }

    private void SaveDetailWidth()
    {
        if (splitDetail.Panel2Collapsed) return;
        var width = splitDetail.ClientSize.Width - splitDetail.SplitterDistance - splitDetail.SplitterWidth;
        if (width < splitDetail.Panel2MinSize) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(InspectorWidthPath)!);
            File.WriteAllText(InspectorWidthPath, width.ToString());
        }
        catch { }
    }

    private readonly record struct ParcelFilter(Guid Id, string Name);
    private readonly record struct StaffFilter(Guid Id, string Name);
}
