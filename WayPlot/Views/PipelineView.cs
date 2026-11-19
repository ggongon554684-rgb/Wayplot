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
        splitDetail.Panel2Collapsed = false;
        splitDetail.SplitterDistance = 420;
        splitDetail.Panel1MinSize = 420;
        splitDetail.Panel2MinSize = 330;
        splitDetail.Panel2Collapsed = true;
        edgeScrollTimer = new System.Windows.Forms.Timer { Interval = 80 };
        edgeScrollTimer.Tick += (_, _) => ScrollBoardFromMouse();
        ApplyTheme();
        txtSearch.TextChanged += (_, _) => { filterTimer.Stop(); filterTimer.Start(); };
        filterTimer.Tick += (_, _) => { filterTimer.Stop(); _ = RunUiAsync(ReloadAsync); };
        cboParcel.SelectedIndexChanged += (_, _) => { if (_filterLoading == 0) _ = RunUiAsync(ReloadAsync); };
        cboAgent.SelectedIndexChanged += (_, _) => { if (_filterLoading == 0) _ = RunUiAsync(ReloadAsync); };
        chkOlder.CheckedChanged += (_, _) => _ = RunUiAsync(ReloadAsync);
        splitDetail.SplitterMoved += (_, _) => SaveDetailWidth();
        btnMove.Click += (_, _) => _ = RunUiAsync(OpenMoveMenuAsync);
        btnLogCall.Click += (_, _) => _ = RunUiAsync(() => LogAsync(InteractionLogKinds.Call));
        btnLogText.Click += (_, _) => _ = RunUiAsync(() => LogAsync(InteractionLogKinds.Text));
        btnLogEmail.Click += (_, _) => _ = RunUiAsync(() => LogAsync(InteractionLogKinds.Email));
        btnLogViewing.Click += (_, _) => _ = RunUiAsync(() => LogAsync(InteractionLogKinds.Viewing));
        btnAddNote.Click += (_, _) => _ = RunUiAsync(() => LogAsync(InteractionLogKinds.Note));
        btnRequestClosed.Click += (_, _) => _ = RunUiAsync(RequestClosedAsync);
        btnSwitchLot.Click += (_, _) => _ = RunUiAsync(SwitchLotAsync);
        btnCloseDetail.Click += (_, _) => CloseDetail();
        btnConfigureStages.Click += (_, _) => _ = RunUiAsync(ConfigureStagesAsync);

        // Keep pipeline column heights in sync when the board panel is resized.
        boardPanel.Resize += (_, _) =>
        {
            var h = Math.Max(400, boardPanel.ClientSize.Height);
            foreach (Control col in boardPanel.Controls)
                col.Height = h;
        };
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
        try
        {
            base.OnLoad(e);
            LoadDetailWidth();
            if (_presenter is not null && !DesignMode)
            {
                await LoadFiltersAsync();
                await ReloadAsync();
            }
        }
        catch (Exception ex)
        {
            AppLog.Error("PipelineView.OnLoad", ex);
            if (!IsDisposed)
                ShowStatus("Could not load the pipeline. Try again.", true);
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
        ButtonStyle.Apply(btnConfigureStages, false);
        ButtonStyle.Apply(btnCloseDetail, false);
    }

    // Incremented while filter combos are being populated so the SelectedIndexChanged
    // handlers ignore those programmatic changes and do not trigger a redundant reload.
    private int _filterLoading;

    private async Task LoadFiltersAsync()
    {
        if (_presenter is null) return;

        _filterLoading++;
        try
        {
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
            else
            {
                // Show a visible hint so the user knows the parcel filter is unavailable.
                ShowStatus("Parcel filter could not be loaded. Pipeline may still load below.", true);
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
        finally
        {
            _filterLoading--;
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
        var colHeight = Math.Max(400, boardPanel.ClientSize.Height);
        foreach (var column in board.Columns)
        {
            var control = new PipelineColumnControl
            {
                Width = 300,
                Height = colHeight,
                Margin = new Padding(0, 0, 8, 0),
                SharedToolTip = _tooltip
            };
            control.SetColumn(column, _presenter?.IsAdmin == true);
            control.CardSelected = card => _ = RunUiAsync(() => SelectCardAsync(card));
            if (_presenter?.CanMutate == true)
            {
                control.MoveRequested = card => _ = RunUiAsync(() => OpenMoveMenuAsync(card.LeadId));
                control.DragStarting = StartDragSafelyAsync;
                control.LeadDropped = (leadId, stageKey) => _ = RunUiAsync(() => DropAsync(leadId, stageKey));
            }
            boardPanel.Controls.Add(control);
        }
        boardPanel.ResumeLayout();
        if (selectedId is { } id)
        {
            var selected = board.Columns.SelectMany(c => c.Cards).FirstOrDefault(c => c.LeadId == id);
            if (selected is not null)
                _ = RunUiAsync(() => SelectCardAsync(selected));
        }
        UpdateRoleActions();
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
                .ToDictionary(move => PipelineStageDefinition.Key(move.TargetStage), move => move)
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

    private async Task<IReadOnlyList<LeadMoveOptionDto>> StartDragSafelyAsync(LeadCardDto card)
    {
        try
        {
            await StartDragAsync(card.LeadId);
            return _dragMoves.Values.ToList();
        }
        catch (Exception)
        {
            if (!IsDisposed)
                ShowStatus("Could not load pipeline actions. Try again.", true);
            return [];
        }
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
                stage.Key == PipelineStageDefinition.Key(move.TargetStage))?.Name
                ?? _presenter.StageDisplayName(move.TargetStage);
            menu.Items.Add(stageName, null, (_, _) =>
                _ = RunUiAsync(() => ExecuteMoveAsync(leadId, move.TargetStage, move)));
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

    private async Task SelectCardAsync(LeadCardDto? card)
    {
        if (_presenter is null || card is null) return;
        _selectedLeadId = card.LeadId;
        ApplyPreferredDetailWidth();
        splitDetail.Panel2Collapsed = false;
        lblContact.Text = card.ContactName;
        lblParcel.Text = card.ParcelLabel;
        lblStage.Text = card.StageName;
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

    private async Task RunUiAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            AppLog.Error("PipelineView.RunUiAsync", ex);
            if (!IsDisposed)
                ShowStatus("The pipeline action could not be completed. Try again.", true);
        }
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
        var selectedStage = _board?.Columns.SelectMany(column => column.Cards)
            .FirstOrDefault(card => card.LeadId == _selectedLeadId)?.Stage;
        var canRequestClosed = selectedStage == _presenter.ClosingStage;
        var requestClosedReason = !canMutate
            ? _presenter.MutationUnavailableReason
            : canRequestClosed
                ? null
                : $"Request Closed is available only from {_presenter.StageDisplayName(_presenter.ClosingStage)}.";
        btnConfigureStages.Visible = _presenter.IsAdmin;
        ButtonStyle.ApplyAvailability([
            new ButtonAvailability(btnMove, canMutate, canMutate, _presenter.MutationUnavailableReason, false),
            new ButtonAvailability(btnLogCall, canMutate, canMutate, _presenter.MutationUnavailableReason, false),
            new ButtonAvailability(btnLogText, canMutate, canMutate, _presenter.MutationUnavailableReason, false),
            new ButtonAvailability(btnLogEmail, canMutate, canMutate, _presenter.MutationUnavailableReason, false),
            new ButtonAvailability(btnLogViewing, canMutate, canMutate, _presenter.MutationUnavailableReason, false),
            new ButtonAvailability(btnAddNote, canMutate, canMutate, _presenter.MutationUnavailableReason, false),
            new ButtonAvailability(btnRequestClosed, canMutate, canMutate && canRequestClosed, requestClosedReason, false),
            new ButtonAvailability(btnSwitchLot, canMutate, canMutate, _presenter.MutationUnavailableReason, false)
        ], _tooltip);
        btnOpenContact.Visible = true;
        btnShowMap.Visible = true;
    }

    private async Task ConfigureStagesAsync()
    {
        if (_presenter is null || !_presenter.IsAdmin) return;
        var stages = await _presenter.GetPipelineStagesForAdminAsync();
        if (!stages.Ok || stages.Value is null)
        {
            ShowStatus(stages.Message, true);
            return;
        }

        using var dialog = new PipelineStagesDialog(stages.Value, _presenter.SavePipelineStagesAsync);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
        ShowStatus(dialog.Result?.Message ?? "Pipeline stages saved.", false);
        await ReloadAsync();
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

    private sealed record ParcelFilter(Guid Id, string Name)
    {
        public override string ToString() => Name;
    }

    private sealed record StaffFilter(Guid Id, string Name)
    {
        public override string ToString() => Name;
    }
}
