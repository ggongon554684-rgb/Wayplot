using System.ComponentModel;
using System.Globalization;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Services.DataCollection;
using WayPlot.Presenters;
using WayPlot.UI;

namespace WayPlot.Views;

/// <summary>The Parcel map tab: the map on the left, the parcel inspector on the right. Layout is in ParcelMapView.Designer.cs.
/// The fact rows in the inspector are created in code (BuildFacts) because they are a fixed list of captions.</summary>
public partial class ParcelMapView : UserControl, IParcelMapView
{
    private static readonly string[] FactCaptions =
    [
        "Price", "Per sqm", "Area", "Zoning", "Reservation",
        "Owner", "Owner phone", "Owner email", "Listing agreement",
        "Tax declaration", "Title no.", "Utilities", "Road access"
    ];

    private readonly List<Label> _factValues = [];
    private IParcelMapControl _map = null!;
    private ParcelMapPresenter? _presenter;
    private ParcelDto? _currentParcel;
    private Guid? _currentParcelId;
    private readonly ToolTip _actionToolTip = new();
    private int _preferredInspectorWidth = 356;
    private bool _settingsLoaded;
    private bool _settingsWarningShown;
    private string _mapMode = "none";
    private int _modeVertexCount;

    public ParcelMapView()
    {
        InitializeComponent();
        _map = mapControl;
        BuildFacts();
        ApplyTheme();
        layoutMain.Panel2Collapsed = true;

        listLeads.Resize += (_, _) => FitLeadColumns();
        listLeads.ItemActivate += async (_, _) => await OpenLeadFileAsync();
        btnAttachLead.Click += (_, _) => AttachLead();
        btnNewParcel.Click += (_, _) => StartDraw("polygon");
        btnPlaceMarker.Click += (_, _) => StartDraw("marker");
        btnUndo.Click += (_, _) => _map.UndoVertex();
        btnFinish.Click += (_, _) => _map.FinishMode();
        btnCancel.Click += (_, _) => _map.CancelMode();
        btnEditDetails.Click += async (_, _) => await EditDetailsAsync();
        btnEditShape.Click += (_, _) => StartShapeEdit();
        btnChangePrice.Click += async (_, _) => await ChangePriceAsync();
        btnReassign.Click += async (_, _) => await ReassignAsync();
        btnArchive.Click += async (_, _) => await ArchiveAsync();
        btnClosePanel.Click += async (_, _) =>
        {
            if (_presenter is not null)
                await _presenter.SelectAsync(null);
        };
        _map.ParcelClicked += async (_, args) =>
        {
            if (_presenter is not null)
                await _presenter.SelectAsync(args.ParcelId);
        };
        _map.MapBackgroundClicked += async (_, _) =>
        {
            if (_presenter is not null)
                await _presenter.SelectAsync(null);
        };
        _map.DrawComplete += async (_, args) => await HandleDrawCompleteAsync(args);
        _map.GeometryEditComplete += async (_, args) => await HandleGeometryEditCompleteAsync(args);
        _map.ModeStateChanged += (_, args) =>
        {
            _mapMode = args.Mode;
            _modeVertexCount = args.VertexCount;
            UpdateMapActions();
        };
        _map.ModeCancelled += (_, _) =>
        {
            _mapMode = "none";
            _modeVertexCount = 0;
            UpdateMapActions();
        };
        layoutMain.SplitterMoved += (_, _) => SaveInspectorWidth();

        ClearParcel();
    }

    /// <summary>Set by the code that creates the screen (see Program.cs). The Designer never saves it.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ParcelMapPresenter? Presenter
    {
        set
        {
            _presenter = value;
            UpdateAttachAction();
            UpdateParcelActions();
            UpdateMapActions();
        }
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        LoadInspectorWidth();
        UpdateAttachAction();
        UpdateParcelActions();
        UpdateMapActions();
        if (_presenter is not null && !DesignMode)
            await _presenter.LoadAsync();
    }

    public void ConfigureMapTiles(MapTileOptions options) => _map.Configure(options);

    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        layoutMain.BackColor = Theme.Page;
        lblParcelLabel.ForeColor = Theme.Navy;
        lblDescription.ForeColor = Theme.Muted;
        lblLeadsHeader.ForeColor = Theme.Navy;
        listLeads.ForeColor = Theme.Text;
        listLeads.BackColor = Theme.Surface;
        ButtonStyle.Apply(btnAttachLead, primary: false);
        ButtonStyle.Apply(btnNewParcel, primary: true);
        ButtonStyle.Apply(btnPlaceMarker, primary: false);
        ButtonStyle.Apply(btnUndo, primary: false);
        ButtonStyle.Apply(btnFinish, primary: true);
        ButtonStyle.Apply(btnCancel, primary: false);
        ButtonStyle.Apply(btnEditDetails, primary: false);
        ButtonStyle.Apply(btnEditShape, primary: false);
        ButtonStyle.Apply(btnChangePrice, primary: false);
        ButtonStyle.Apply(btnReassign, primary: false);
        ButtonStyle.Apply(btnArchive, primary: false);
    }

    private void BuildFacts()
    {
        facts.SuspendLayout();
        facts.RowStyles.Clear();
        facts.RowCount = FactCaptions.Length;

        for (var i = 0; i < FactCaptions.Length; i++)
        {
            facts.RowStyles.Add(new RowStyle());

            var caption = new Label
            {
                AutoSize = true,
                Text = FactCaptions[i],
                ForeColor = Theme.Muted,
                Margin = new Padding(0, 0, LogicalToDeviceUnits(14), LogicalToDeviceUnits(6)),
                UseMnemonic = false   // an owner such as "Santos & Sons" must not turn the & into an underline
            };
            var value = new Label
            {
                AutoSize = true,
                Text = string.Empty,
                ForeColor = Theme.Text,
                Margin = new Padding(0, 0, 0, LogicalToDeviceUnits(6)),
                MaximumSize = new Size(LogicalToDeviceUnits(170), 0),
                UseMnemonic = false
            };

            facts.Controls.Add(caption, 0, i);
            facts.Controls.Add(value, 1, i);
            _factValues.Add(value);
        }

        facts.ResumeLayout(true);
    }

    private void FitLeadColumns()
    {
        colStage.Width = LogicalToDeviceUnits(100);
        colContact.Width = Math.Max(LogicalToDeviceUnits(60), listLeads.ClientSize.Width - colStage.Width);
    }

    // ---- IParcelMapView ----

    public void ShowMessage(string? text)
    {
        if (IsDisposed) return;
        _map.SetMessage(text);
    }

    public void ShowParcels(string featureCollectionJson, int featureCount, int unplotted)
    {
        if (IsDisposed) return;
        _map.LoadParcels(featureCollectionJson);
        _map.SetNote(unplotted > 0
            ? $"{unplotted} parcel{(unplotted == 1 ? "" : "s")} not shown (no map position)."
            : null);
    }

    public void ShowParcel(ParcelDto parcel, IReadOnlyList<LeadDto>? leads)
    {
        if (IsDisposed) return;

        _currentParcelId = parcel.Id;
        _currentParcel = parcel;
        var wasCollapsed = layoutMain.Panel2Collapsed;
        layoutMain.Panel2Collapsed = false;
        if (wasCollapsed)
            ApplyPreferredInspectorWidth();
        detailsLayout.Visible = true;
        _map.SelectParcel(parcel.Id);

        lblParcelLabel.Text = parcel.Label;
        lblParcelStatus.Text = "● " + parcel.Status;
        lblParcelStatus.ForeColor = ParcelColors.For(parcel.Status);

        SetFact(0, Peso(parcel.Price));
        SetFact(1, Peso(parcel.PricePerSqm) + " / sqm");
        SetFact(2, parcel.AreaSqm.ToString("N0", CultureInfo.InvariantCulture) + " sqm");
        SetFact(3, parcel.Zoning);
        SetFact(4, $"{Peso(parcel.ReservationFee)} ({parcel.ReservationPercent.ToString("0.##", CultureInfo.InvariantCulture)}%)");
        SetFact(5, parcel.OwnerName);
        SetFact(6, parcel.OwnerPhone);
        SetFact(7, parcel.OwnerEmail);
        SetFact(8, parcel.OwnerListingAgreementOnFile ? "On file" : "Not on file");
        SetFact(9, parcel.TaxDeclarationNo);
        SetFact(10, parcel.TitleNo);
        SetFact(11, parcel.Utilities);
        SetFact(12, parcel.RoadAccess);

        lblDescription.Text = string.IsNullOrWhiteSpace(parcel.Description) ? "No description." : parcel.Description;
        UpdateAttachAction();
        UpdateParcelActions();

        listLeads.BeginUpdate();
        listLeads.Items.Clear();
        if (leads is null)
        {
            lblLeadsHeader.Text = "Leads (loading...)";
        }
        else
        {
            lblLeadsHeader.Text = leads.Count == 1 ? "1 lead" : $"{leads.Count} leads";
            foreach (var lead in leads)
            {
                var item = new ListViewItem(lead.ContactName) { Tag = lead.Id };   // Tag: step 5b opens the lead file from it
                item.SubItems.Add(lead.Stage.ToString());
                listLeads.Items.Add(item);
            }
        }
        listLeads.EndUpdate();
        FitLeadColumns();
    }

    public void ShowLeadsError(string message)
    {
        if (IsDisposed) return;
        listLeads.Items.Clear();
        lblLeadsHeader.Text = message;
    }

    public void ClearParcel()
    {
        if (IsDisposed) return;
        _currentParcelId = null;
        _currentParcel = null;
        _map.SelectParcel(null);
        UpdateAttachAction();
        UpdateParcelActions();
        layoutMain.Panel2Collapsed = true;
    }

    public void FocusParcel(Guid parcelId) => _map.FitParcel(parcelId);

    private void AttachLead()
    {
        if (_presenter is null || _currentParcelId is not { } parcelId) return;

        using var dialog = new AttachLeadDialog
        {
            SearchHandler = _presenter.SearchContactsAsync,
            SaveHandler = contactId => _presenter.AttachLeadAsync(contactId, parcelId)
        };
        dialog.ShowDialog(FindForm());
    }

    private void UpdateAttachAction()
    {
        var visible = _presenter?.CanShowAttachLead == true;
        var canAttach = _presenter?.CanAttachLeads == true;
        ButtonStyle.ApplyAvailability(
            [new ButtonAvailability(btnAttachLead, visible, canAttach,
                canAttach ? null : _presenter?.AttachUnavailableReason, Primary: false)],
            _actionToolTip);
    }

    private void UpdateParcelActions()
    {
        var selected = _currentParcelId is not null && _currentParcel is not null;
        var showEdit = selected && _presenter?.CanShowParcelActions == true;
        var editPermission = showEdit
            ? _presenter!.GetEditPermission(_currentParcelId)
            : RuleResult.Fail("Select a parcel first.");
        var shapePermission = showEdit
            ? _presenter!.GetShapeEditPermission(_currentParcelId)
            : RuleResult.Fail("Select a parcel first.");
        var showAdmin = selected && _presenter?.CanShowAdminParcelActions == true;

        ButtonStyle.ApplyAvailability(
        [
            new ButtonAvailability(btnEditDetails, showEdit, editPermission.Ok,
                editPermission.Ok ? null : editPermission.Message, Primary: false),
            new ButtonAvailability(btnEditShape, showEdit, shapePermission.Ok,
                shapePermission.Ok ? null : shapePermission.Message, Primary: false),
            new ButtonAvailability(btnChangePrice, showAdmin, showAdmin,
                showAdmin ? null : "Only the Tenant Admin can change parcel prices.", Primary: false),
            new ButtonAvailability(btnReassign, showAdmin, showAdmin,
                showAdmin ? null : "Only the Tenant Admin can reassign parcels.", Primary: false),
            new ButtonAvailability(btnArchive, showAdmin, showAdmin,
                showAdmin ? null : "Only the Tenant Admin can archive parcels.", Primary: false)
        ], _actionToolTip);
    }

    private void UpdateMapActions()
    {
        var canPlot = _presenter?.CanPlotParcel == true;
        var active = _mapMode != "none";
        var enabledReason = active ? "Finish or cancel the current map mode first." : null;
        var polygonMode = _mapMode == "polygon";
        var canFinish = active && (_mapMode != "polygon" || _modeVertexCount >= 3);
        var finishReason = _mapMode == "polygon" && _modeVertexCount < 3
            ? "Add at least 3 boundary points before finishing."
            : null;
        var undoReason = polygonMode
            ? "Add a point before undoing."
            : "Undo is available only while drawing a polygon.";

        ButtonStyle.ApplyAvailability(
        [
            new ButtonAvailability(btnNewParcel, canPlot, canPlot && !active,
                enabledReason ?? _presenter?.PlotPermission.Message, Primary: true),
            new ButtonAvailability(btnPlaceMarker, canPlot, canPlot && !active,
                enabledReason ?? _presenter?.PlotPermission.Message, Primary: false),
            new ButtonAvailability(btnUndo, canPlot && active, polygonMode && _modeVertexCount > 0,
                undoReason, Primary: false),
            new ButtonAvailability(btnFinish, canPlot && active, canFinish,
                finishReason ?? "Finish is available during a map mode.", Primary: true),
            new ButtonAvailability(btnCancel, canPlot && active, active,
                "Cancel the current map mode.", Primary: false)
        ], _actionToolTip);

        lblMapHint.Visible = canPlot && active;
        lblMapHint.Text = _mapMode switch
        {
            "polygon" => _modeVertexCount < 3
                ? $"Click to add boundary points ({_modeVertexCount}; at least 3). Enter/double-click to finish; Backspace to undo; Esc to cancel."
                : $"Click to add boundary points ({_modeVertexCount}). Enter/double-click to finish; Backspace to undo; Esc to cancel.",
            "marker" => "Click to place the marker, or Esc to cancel.",
            "editGeometry" => "Drag the vertices. Enter or Finish to save; Esc to cancel.",
            _ => string.Empty
        };
    }

    private void StartDraw(string mode)
    {
        if (_presenter is null || _mapMode != "none")
            return;
        var permission = _presenter.PlotPermission;
        if (!permission.Ok)
        {
            ShowActionMessage(permission.Message);
            return;
        }
        _map.StartDraw(mode);
    }

    private void StartShapeEdit()
    {
        if (_presenter is null || _currentParcelId is not { } parcelId)
            return;
        var permission = _presenter.GetShapeEditPermission(parcelId);
        if (!permission.Ok)
        {
            ShowActionMessage(permission.Message);
            return;
        }
        _map.StartEditGeometry(parcelId);
    }

    private async Task HandleDrawCompleteAsync(ParcelGeometryEventArgs args)
    {
        if (_presenter is null)
            return;

        var permission = _presenter.PlotPermission;
        if (!permission.Ok)
        {
            ShowActionMessage(permission.Message);
            return;
        }

        var validation = ParcelGeometry.Validate(args.Kind, args.Points);
        if (!validation.Ok)
        {
            ShowActionMessage(validation.Message);
            return;
        }

        try
        {
            var ownersResult = await _presenter.ListOwnersAsync();
            if (IsDisposed)
                return;
            if (!ownersResult.Ok || ownersResult.Value is null)
            {
                ShowActionMessage(ownersResult.Message);
                return;
            }

            IReadOnlyList<ParcelAgentOption>? agents = null;
            if (_presenter.CanShowAdminParcelActions)
            {
                var agentsResult = await _presenter.ListActiveAgentsAsync();
                if (!agentsResult.Ok || agentsResult.Value is null)
                {
                    ShowActionMessage(agentsResult.Message);
                    return;
                }
                agents = agentsResult.Value;
            }

            var geoJson = args.Kind == "polygon"
                ? ParcelGeometry.BuildPolygonGeoJson(args.Points)
                : null;
            var marker = args.Kind == "marker" ? args.Points[0] : (ParcelCoordinate?)null;
            var areaSqm = args.Kind == "polygon" ? ParcelGeometry.ComputeAreaSqm(args.Points) : (decimal?)null;
            using var dialog = new ParcelDialog(
                ownersResult.Value,
                agents,
                null,
                areaSqm,
                geoJson,
                marker?.Latitude,
                marker?.Longitude)
            {
                CreateOwnerHandler = _presenter.CreateOwnerAsync
            };
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK || dialog.CreateRequest is not { } request)
                return;

            var result = await _presenter.SaveDrawnParcelAsync(request, args.Kind, args.Points);
            ShowServiceResult(result.Ok, result.Message);
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
                ShowActionMessage($"The parcel could not be created. {ex.Message}");
        }
    }

    private async Task HandleGeometryEditCompleteAsync(ParcelGeometryEventArgs args)
    {
        if (_presenter is null || args.ParcelId is not { } parcelId)
            return;

        var validation = ParcelGeometry.Validate(args.Kind, args.Points);
        if (!validation.Ok)
        {
            ShowActionMessage(validation.Message);
            return;
        }

        try
        {
            var result = await _presenter.SaveEditedGeometryAsync(parcelId, args.Kind, args.Points);
            ShowServiceResult(result.Ok, result.Message);
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
                ShowActionMessage($"The parcel shape could not be saved. {ex.Message}");
        }
    }

    private async Task EditDetailsAsync()
    {
        if (_presenter is null || _currentParcel is not { } parcel)
            return;

        var permission = _presenter.GetEditPermission(parcel.Id);
        if (!permission.Ok)
        {
            ShowActionMessage(permission.Message);
            return;
        }

        try
        {
            var ownersResult = await _presenter.ListOwnersAsync();
            if (IsDisposed)
                return;
            if (!ownersResult.Ok || ownersResult.Value is null)
            {
                ShowActionMessage(ownersResult.Message);
                return;
            }

            using var dialog = new ParcelDialog(
                ownersResult.Value, null, parcel, null, null, null, null)
            {
                CreateOwnerHandler = _presenter.CreateOwnerAsync
            };
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK || dialog.UpdateRequest is not { } request)
                return;

            var result = await _presenter.UpdateParcelAsync(request);
            ShowServiceResult(result.Ok, result.Message);
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
                ShowActionMessage($"The parcel details could not be saved. {ex.Message}");
        }
    }

    private async Task ChangePriceAsync()
    {
        if (_presenter is null || _currentParcel is not { } parcel)
            return;
        var permission = _presenter.CanShowAdminParcelActions;
        if (!permission)
        {
            ShowActionMessage("Only the Tenant Admin can change parcel prices.");
            return;
        }

        using var dialog = new ChangePriceDialog(parcel.Id, parcel.Price, parcel.ReservationFee);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK || dialog.Request is not { } request)
            return;
        try
        {
            var result = await _presenter.ChangeParcelPriceAsync(request);
            ShowServiceResult(result.Ok, result.Message);
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
                ShowActionMessage($"The parcel price could not be saved. {ex.Message}");
        }
    }

    private async Task ReassignAsync()
    {
        if (_presenter is null || _currentParcel is not { } parcel)
            return;
        if (!_presenter.CanShowAdminParcelActions)
        {
            ShowActionMessage("Only the Tenant Admin can reassign parcels.");
            return;
        }

        try
        {
            var agentsResult = await _presenter.ListActiveAgentsAsync();
            if (IsDisposed)
                return;
            if (!agentsResult.Ok || agentsResult.Value is null)
            {
                ShowActionMessage(agentsResult.Message);
                return;
            }
            if (agentsResult.Value.Count == 0)
            {
                ShowActionMessage("There are no active Agents to assign.");
                return;
            }

            using var dialog = new ReassignDialog(parcel.Id, agentsResult.Value, parcel.AssignedStaffId);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK || dialog.Request is not { } request)
                return;
            var result = await _presenter.ReassignParcelAsync(request);
            ShowServiceResult(result.Ok, result.Message);
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
                ShowActionMessage($"The parcel could not be reassigned. {ex.Message}");
        }
    }

    private async Task ArchiveAsync()
    {
        if (_presenter is null || _currentParcel is not { } parcel)
            return;
        if (!_presenter.CanShowAdminParcelActions)
        {
            ShowActionMessage("Only the Tenant Admin can archive parcels.");
            return;
        }

        if (MessageBox.Show(
                FindForm(),
                $"Archive \"{parcel.Label}\"? This is a soft delete; its label remains reserved.",
                "Archive parcel",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        try
        {
            var result = await _presenter.ArchiveParcelAsync(parcel.Id);
            ShowServiceResult(result.Ok, result.Message);
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
                ShowActionMessage($"The parcel could not be archived. {ex.Message}");
        }
    }

    private void ShowServiceResult(bool ok, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;
        if (ok)
            _map.SetMessage(message);
        else
            ShowActionMessage(message);
    }

    private void ShowActionMessage(string message)
    {
        if (!IsDisposed)
            MessageBox.Show(FindForm(), message, "WayPlot CRM", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
    }

    private async Task OpenLeadFileAsync()
    {
        if (_presenter is null || listLeads.SelectedItems.Count == 0 ||
            listLeads.SelectedItems[0].Tag is not Guid leadId)
            return;

        var result = await _presenter.GetLeadFileAsync(leadId);
        if (IsDisposed) return;
        if (!result.Ok || result.Value is null)
        {
            MessageBox.Show(FindForm(), result.Message, "WayPlot CRM", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new LeadFileDialog(result.Value)
        {
            MoveHandler = _presenter.MoveStageAsync,
            LoadHandler = _presenter.GetLeadFileAsync,
            LeadSearchHandler = _presenter.SearchLeadsAsync,
            InteractionSaveHandler = _presenter.LogInteractionAsync
        };
        dialog.ConfigureMutationActions(
            _presenter.CanMutateLeads,
            _presenter.CanMutateLeads,
            _presenter.MutationUnavailableReason);
        dialog.ShowDialog(FindForm());
    }

    private void SetFact(int index, string? text) =>
        _factValues[index].Text = string.IsNullOrWhiteSpace(text) ? "—" : text;

    private static string Peso(decimal amount) => "₱" + amount.ToString("N0", CultureInfo.InvariantCulture);

    private static string InspectorWidthPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WayPlot",
        "parcel-map.settings");

    private void LoadInspectorWidth()
    {
        _settingsLoaded = true;
        try
        {
            if (File.Exists(InspectorWidthPath)
                && int.TryParse(File.ReadAllText(InspectorWidthPath), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var width)
                && width >= layoutMain.Panel2MinSize)
                _preferredInspectorWidth = width;
        }
        catch (IOException ex)
        {
            ShowSettingsWarning(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            ShowSettingsWarning(ex.Message);
        }
    }

    private void ApplyPreferredInspectorWidth()
    {
        var maxInspectorWidth = layoutMain.ClientSize.Width
            - layoutMain.Panel1MinSize
            - layoutMain.SplitterWidth;
        if (maxInspectorWidth < layoutMain.Panel2MinSize)
            return;

        var inspectorWidth = Math.Clamp(_preferredInspectorWidth, layoutMain.Panel2MinSize, maxInspectorWidth);
        layoutMain.SplitterDistance = layoutMain.ClientSize.Width - inspectorWidth - layoutMain.SplitterWidth;
    }

    private void SaveInspectorWidth()
    {
        if (!_settingsLoaded || layoutMain.Panel2Collapsed || IsDisposed)
            return;

        var width = layoutMain.ClientSize.Width - layoutMain.SplitterDistance - layoutMain.SplitterWidth;
        if (width < layoutMain.Panel2MinSize)
            return;

        _preferredInspectorWidth = width;
        try
        {
            var path = InspectorWidthPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, width.ToString(CultureInfo.InvariantCulture));
        }
        catch (IOException ex)
        {
            ShowSettingsWarning(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            ShowSettingsWarning(ex.Message);
        }
    }

    private void ShowSettingsWarning(string reason)
    {
        if (_settingsWarningShown || IsDisposed)
            return;
        _settingsWarningShown = true;
        MessageBox.Show(
            FindForm(),
            $"The parcel map panel width could not be saved or loaded. {reason}",
            "WayPlot CRM",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }
}
