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
    private readonly List<ParcelPhotoThumbnailDto> _parcelPhotos = [];
    private IParcelMapControl _map = null!;
    private ParcelMapPresenter? _presenter;
    private ParcelDto? _currentParcel;
    private Guid? _currentParcelId;
    private readonly ToolTip _actionToolTip = new();
    private readonly ToolTip _photoToolTip = new();
    private CancellationTokenSource? _photoLoadCancellation;
    private Guid? _selectedPhotoId;
    private bool _photoOperationInProgress;
    private int _preferredInspectorWidth = 356;
    private bool _settingsLoaded;
    private bool _settingsWarningShown;
    private string _mapMode = "none";
    private int _modeVertexCount;
    private bool _savingGeometry;
    private Guid? _pendingParcelId;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Guid? PendingParcelId
    {
        get => _pendingParcelId;
        set
        {
            _pendingParcelId = value;
            if (IsHandleCreated && value is { } id && _presenter is not null)
                _ = SelectPendingParcelAsync(id);
        }
    }

    public ParcelMapView()
    {
        InitializeComponent();
        _map = mapControl;
        BuildFacts();
        ApplyTheme();
        layoutMain.Panel2Collapsed = true;

        transientStatusTimer.Tick += (_, _) => ClearTransientStatus();
        listLeads.Resize += (_, _) => FitLeadColumns();
        listLeads.ItemActivate += (_, _) => AsyncUiEvent.Run(this, OpenLeadFileAsync, "Could not open the lead. Try again.");
        btnAttachLead.Click += (_, _) => AttachLead();
        btnNewParcel.Click += (_, _) => StartDraw();
        btnUndo.Click += (_, _) => _map.UndoVertex();
        btnFinish.Click += (_, _) => _map.FinishMode();
        btnCancel.Click += (_, _) => _map.CancelMode();
        btnEditDetails.Click += (_, _) => AsyncUiEvent.Run(this, EditDetailsAsync, "Could not edit parcel details. Try again.");
        btnEditShape.Click += (_, _) => StartShapeEdit();
        btnChangePrice.Click += (_, _) => AsyncUiEvent.Run(this, ChangePriceAsync, "Could not change parcel price. Try again.");
        btnReassign.Click += (_, _) => AsyncUiEvent.Run(this, ReassignAsync, "Could not reassign the parcel. Try again.");
        btnArchive.Click += (_, _) => AsyncUiEvent.Run(this, ArchiveAsync, "Could not archive the parcel. Try again.");
        btnAddPhotos.Click += (_, _) => AsyncUiEvent.Run(this, AddPhotosAsync, "Could not add parcel photos. Try again.");
        btnRemovePhoto.Click += (_, _) => AsyncUiEvent.Run(this, RemovePhotoAsync, "Could not remove the photo. Try again.");
        btnClosePanel.Click += (_, _) => AsyncUiEvent.Run(this, async () =>
        {
            if (_presenter is not null)
                await _presenter.SelectAsync(null);
        }, "Could not close the parcel panel. Try again.");
        _map.ParcelClicked += (_, args) => AsyncUiEvent.Run(this, async () =>
        {
            if (_presenter is not null)
                await _presenter.SelectAsync(args.ParcelId);
        }, "Could not select the parcel. Try again.");
        _map.MapBackgroundClicked += (_, _) => AsyncUiEvent.Run(this, async () =>
        {
            if (_presenter is not null)
                await _presenter.SelectAsync(null);
        }, "Could not clear the parcel selection. Try again.");
        _map.DrawComplete += (_, args) => AsyncUiEvent.Run(this, () => HandleDrawCompleteAsync(args), "Could not save the parcel boundary. Try again.");
        _map.GeometryEditComplete += (_, args) => AsyncUiEvent.Run(this, () => HandleGeometryEditCompleteAsync(args), "Could not save the parcel boundary. Try again.");
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
        try
        {
            base.OnLoad(e);
            LoadInspectorWidth();
            UpdateAttachAction();
            UpdateParcelActions();
            UpdateMapActions();
            if (_presenter is not null && !DesignMode)
            {
                await _presenter.LoadAsync();
                if (_pendingParcelId is { } parcelId)
                {
                    _pendingParcelId = null;
                    await _presenter.SelectAsync(parcelId);
                }
            }
        }
        catch (Exception)
        {
            if (!IsDisposed)
                ShowTransientStatus("Could not load parcels. Try again.");
        }
    }

    public void ConfigureMapTiles(MapTileOptions options) => _map.Configure(options);

    private async Task SelectPendingParcelAsync(Guid parcelId)
    {
        if (_presenter is null) return;
        await _presenter.LoadAsync();
        if (IsDisposed) return;
        await _presenter.SelectAsync(parcelId);
        _pendingParcelId = null;
    }

    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        layoutMain.BackColor = Theme.Page;
        lblParcelLabel.ForeColor = Theme.Navy;
        lblDescription.ForeColor = Theme.Muted;
        lblMapStatus.ForeColor = Theme.Muted;
        lblLeadsHeader.ForeColor = Theme.Navy;
        listLeads.ForeColor = Theme.Text;
        listLeads.BackColor = Theme.Surface;
        ButtonStyle.Apply(btnAttachLead, primary: false);
        ButtonStyle.Apply(btnNewParcel, primary: true);
        ButtonStyle.Apply(btnUndo, primary: false);
        ButtonStyle.Apply(btnFinish, primary: true);
        ButtonStyle.Apply(btnCancel, primary: false);
        ButtonStyle.Apply(btnEditDetails, primary: false);
        ButtonStyle.Apply(btnEditShape, primary: false);
        ButtonStyle.Apply(btnChangePrice, primary: false);
        ButtonStyle.Apply(btnReassign, primary: false);
        ButtonStyle.Apply(btnArchive, primary: false);
        ButtonStyle.Apply(btnAddPhotos, primary: false);
        ButtonStyle.Apply(btnRemovePhoto, primary: false);
        lblPhotosHeader.ForeColor = Theme.Navy;
        lblPhotoStatus.ForeColor = Theme.Muted;
        flowPhotos.BackColor = Theme.Surface;
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

        var parcelChanged = _currentParcelId != parcel.Id;
        if (parcelChanged)
        {
            ClearTransientStatus();
            CancelPhotoLoad();
            ClearPhotoThumbnails();
            _selectedPhotoId = null;
        }
        _currentParcelId = parcel.Id;
        _currentParcel = parcel;
        var wasCollapsed = layoutMain.Panel2Collapsed;
        if (wasCollapsed)
            ApplyPreferredInspectorWidth();
        layoutMain.Panel2Collapsed = false;
        layoutMain.PerformLayout();
        detailsLayout.Visible = true;
        _map.SelectParcel(parcel.Id);
        _map.FitParcel(parcel.Id);

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
        UpdatePhotoActions();
        if (parcelChanged)
            StartPhotoLoad(parcel.Id);

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
        ClearTransientStatus();
        _currentParcelId = null;
        _currentParcel = null;
        CancelPhotoLoad();
        ClearPhotoThumbnails();
        _selectedPhotoId = null;
        lblPhotoStatus.Text = "Select a parcel to view photos.";
        progressPhotos.Visible = false;
        _map.SelectParcel(null);
        UpdateAttachAction();
        UpdateParcelActions();
        UpdatePhotoActions();
        layoutMain.Panel2Collapsed = true;
    }

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

    private void UpdatePhotoActions()
    {
        var visible = _presenter?.CanShowPhotoActions == true;
        var parcelSelected = _currentParcelId is not null;
        var permission = visible
            ? _presenter!.GetPhotoMutationPermission(_currentParcelId)
            : RuleResult.Fail("Photo changes are unavailable for your role.");
        var canChangePhotos = parcelSelected && permission.Ok && !_photoOperationInProgress;
        var disabledReason = parcelSelected
            ? permission.Message
            : "Select a parcel first.";
        ButtonStyle.ApplyAvailability(
        [
            new ButtonAvailability(btnAddPhotos, visible, canChangePhotos,
                canChangePhotos ? null : disabledReason, Primary: false),
            new ButtonAvailability(btnRemovePhoto, visible,
                canChangePhotos && _selectedPhotoId is not null,
                canChangePhotos
                    ? _selectedPhotoId is null ? "Select a photo first." : null
                    : disabledReason,
                Primary: false)
        ], _photoToolTip);
    }

    private void StartPhotoLoad(Guid parcelId)
    {
        CancelPhotoLoad();
        _photoLoadCancellation = new CancellationTokenSource();
        _ = LoadPhotoThumbnailsAsync(parcelId, _photoLoadCancellation.Token);
    }

    private async Task RefreshPhotoThumbnailsAsync(Guid parcelId)
    {
        CancelPhotoLoad();
        _photoLoadCancellation = new CancellationTokenSource();
        await LoadPhotoThumbnailsAsync(parcelId, _photoLoadCancellation.Token);
    }

    private async Task LoadPhotoThumbnailsAsync(Guid parcelId, CancellationToken cancellationToken)
    {
        var presenter = _presenter;
        if (presenter is null || IsDisposed)
            return;

        lblPhotoStatus.Text = "Loading photos...";
        progressPhotos.Visible = true;
        try
        {
            var result = await Task.Run(
                () => presenter.ListPhotosAsync(parcelId, cancellationToken),
                cancellationToken);
            if (cancellationToken.IsCancellationRequested || IsDisposed || _currentParcelId != parcelId)
                return;
            if (!result.Ok || result.Value is null)
            {
                lblPhotoStatus.Text = result.Message;
                return;
            }

            var photos = result.Value;
            var loaded = await Task.Run(
                () => DecodeThumbnails(photos, cancellationToken),
                cancellationToken);
            if (cancellationToken.IsCancellationRequested || IsDisposed || _currentParcelId != parcelId)
            {
                DisposeLoadedThumbnails(loaded);
                return;
            }

            DisplayPhotoThumbnails(loaded);
            lblPhotoStatus.Text = photos.Count == 0
                ? "No photos yet."
                : $"{photos.Count} photo{(photos.Count == 1 ? string.Empty : "s")}.";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            if (!IsDisposed && _currentParcelId == parcelId)
                lblPhotoStatus.Text = $"Could not load parcel photos. {ex.Message}";
        }
        finally
        {
            if (!IsDisposed && _currentParcelId == parcelId)
                progressPhotos.Visible = _photoOperationInProgress;
        }
    }

    private static Image DecodeThumbnail(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var image = Image.FromStream(stream);
        return new Bitmap(image);
    }

    private static List<(ParcelPhotoThumbnailDto Photo, Image Image)> DecodeThumbnails(
        IReadOnlyList<ParcelPhotoThumbnailDto> photos,
        CancellationToken cancellationToken)
    {
        var loaded = new List<(ParcelPhotoThumbnailDto Photo, Image Image)>(photos.Count);
        try
        {
            foreach (var photo in photos)
            {
                cancellationToken.ThrowIfCancellationRequested();
                loaded.Add((photo, DecodeThumbnail(photo.ThumbnailBytes)));
            }
            return loaded;
        }
        catch
        {
            DisposeLoadedThumbnails(loaded);
            throw;
        }
    }

    private void DisplayPhotoThumbnails(
        IReadOnlyList<(ParcelPhotoThumbnailDto Photo, Image Image)> thumbnails)
    {
        ClearPhotoThumbnails();
        _parcelPhotos.Clear();
        foreach (var (photo, image) in thumbnails)
        {
            _parcelPhotos.Add(photo);
            var picture = new PictureBox
            {
                BackColor = Theme.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand,
                Image = image,
                Margin = new Padding(3),
                Size = new Size(LogicalToDeviceUnits(78), LogicalToDeviceUnits(78)),
                SizeMode = PictureBoxSizeMode.Zoom,
                Tag = photo
            };
            _photoToolTip.SetToolTip(picture, $"{photo.FileName} ({photo.SizeBytes:N0} bytes)");
            picture.Click += (_, _) => SelectPhoto(picture);
            picture.DoubleClick += (_, _) => AsyncUiEvent.Run(this, () => OpenPhotoViewerAsync(photo.Id), "Could not open the photo. Try again.");
            flowPhotos.Controls.Add(picture);
        }
        UpdatePhotoActions();
    }

    private void SelectPhoto(PictureBox picture)
    {
        if (picture.Tag is not ParcelPhotoThumbnailDto photo)
            return;

        _selectedPhotoId = photo.Id;
        foreach (var control in flowPhotos.Controls)
        {
            if (control is PictureBox thumbnail && thumbnail.Tag is ParcelPhotoThumbnailDto item)
                thumbnail.BackColor = item.Id == photo.Id ? Theme.SurfaceHover : Theme.Surface;
        }
        UpdatePhotoActions();
    }

    private void ClearPhotoThumbnails()
    {
        foreach (Control control in flowPhotos.Controls)
        {
            if (control is PictureBox picture)
            {
                _photoToolTip.SetToolTip(picture, string.Empty);
                picture.Image?.Dispose();
                picture.Image = null;
            }
            control.Dispose();
        }
        flowPhotos.Controls.Clear();
        _parcelPhotos.Clear();
    }

    private static void DisposeLoadedThumbnails(IEnumerable<(ParcelPhotoThumbnailDto Photo, Image Image)> thumbnails)
    {
        foreach (var (_, image) in thumbnails)
            image.Dispose();
    }

    private void CancelPhotoLoad()
    {
        _photoLoadCancellation?.Cancel();
        _photoLoadCancellation?.Dispose();
        _photoLoadCancellation = null;
    }

    private void DisposePhotoResources()
    {
        CancelPhotoLoad();
        if (flowPhotos is not null)
            ClearPhotoThumbnails();
        _photoToolTip.Dispose();
        _actionToolTip.Dispose();
    }

    private async Task AddPhotosAsync()
    {
        if (_presenter is null || _currentParcelId is not { } parcelId)
            return;

        var pattern = string.Join(";", ParcelPhotoLimits.SupportedExtensions.Select(extension => "*" + extension));
        using var dialog = new OpenFileDialog
        {
            Filter = $"Image files ({pattern})|{pattern}",
            Multiselect = true,
            Title = "Add parcel photos"
        };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
            return;

        _photoOperationInProgress = true;
        progressPhotos.Visible = true;
        UpdatePhotoActions();
        try
        {
            for (var index = 0; index < dialog.FileNames.Length; index++)
            {
                if (IsDisposed || _currentParcelId != parcelId)
                    break;

                var filePath = dialog.FileNames[index];
                lblPhotoStatus.Text = $"Processing {index + 1} of {dialog.FileNames.Length}: {Path.GetFileName(filePath)}";
                try
                {
                    var processed = await Task.Run(() => PhotoProcessor.Process(filePath));
                    if (!processed.Ok || processed.Value is null)
                    {
                        ShowActionMessage($"{Path.GetFileName(filePath)}: {processed.Message}");
                        continue;
                    }

                    var result = await _presenter.AddPhotoAsync(
                        parcelId,
                        Path.GetFileName(filePath),
                        processed.Value.ImageBytes,
                        processed.Value.ThumbnailBytes);
                    if (!result.Ok)
                        ShowActionMessage($"{Path.GetFileName(filePath)}: {result.Message}");
                }
                catch (Exception ex)
                {
                    ShowActionMessage($"{Path.GetFileName(filePath)}: the photo could not be added. {ex.Message}");
                }
            }
        }
        finally
        {
            _photoOperationInProgress = false;
            if (!IsDisposed && _currentParcelId == parcelId)
            {
                progressPhotos.Visible = false;
                await RefreshPhotoThumbnailsAsync(parcelId);
                UpdatePhotoActions();
            }
        }
    }

    private async Task RemovePhotoAsync()
    {
        if (_presenter is null || _currentParcelId is not { } parcelId || _selectedPhotoId is not { } photoId)
            return;

        var photo = _parcelPhotos.FirstOrDefault(item => item.Id == photoId);
        if (photo is null)
            return;
        if (MessageBox.Show(
                FindForm(),
                $"Remove \"{photo.FileName}\" from this parcel?",
                "Remove parcel photo",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        _photoOperationInProgress = true;
        progressPhotos.Visible = true;
        lblPhotoStatus.Text = "Removing photo...";
        UpdatePhotoActions();
        try
        {
            var result = await _presenter.RemovePhotoAsync(parcelId, photoId);
            if (!result.Ok)
            {
                ShowActionMessage(result.Message);
                return;
            }

            _selectedPhotoId = null;
            await RefreshPhotoThumbnailsAsync(parcelId);
        }
        catch (Exception ex)
        {
            ShowActionMessage($"The photo could not be removed. {ex.Message}");
        }
        finally
        {
            _photoOperationInProgress = false;
            if (!IsDisposed && _currentParcelId == parcelId)
            {
                progressPhotos.Visible = false;
                UpdatePhotoActions();
            }
        }
    }

    private Task OpenPhotoViewerAsync(Guid photoId)
    {
        if (_presenter is null)
            return Task.CompletedTask;
        var index = _parcelPhotos.FindIndex(photo => photo.Id == photoId);
        if (index < 0)
            return Task.CompletedTask;

        var presenter = _presenter;
        using var dialog = new PhotoViewerDialog(
            _parcelPhotos.ToArray(),
            index,
            selectedPhotoId => presenter.GetPhotoImageAsync(selectedPhotoId));
        dialog.ShowDialog(FindForm());
        return Task.CompletedTask;
    }

    private void UpdateMapActions()
    {
        var canPlot = _presenter?.CanPlotParcel == true;
        var active = _mapMode != "none";
        var canStart = canPlot && !active && !_savingGeometry;
        var enabledReason = active ? "Finish or cancel the current map mode first." : null;
        var polygonMode = _mapMode == "polygon";
        var canFinish = active && !_savingGeometry && (_mapMode != "polygon" || _modeVertexCount >= 3);
        var finishReason = _mapMode == "polygon" && _modeVertexCount < 3
            ? "Add at least 3 boundary points before finishing."
            : null;
        var undoReason = polygonMode
            ? "Add a point before undoing."
            : "Undo is available only while drawing a polygon.";

        ButtonStyle.ApplyAvailability(
        [
            new ButtonAvailability(btnNewParcel, canPlot, canStart,
                enabledReason ?? _presenter?.PlotPermission.Message, Primary: true),
            new ButtonAvailability(btnUndo, canPlot && active, polygonMode && _modeVertexCount > 0 && !_savingGeometry,
                undoReason, Primary: false),
            new ButtonAvailability(btnFinish, canPlot && active, canFinish,
                finishReason ?? "Finish is available during a map mode.", Primary: true),
            new ButtonAvailability(btnCancel, canPlot && active, active && !_savingGeometry,
                "Cancel the current map mode.", Primary: false)
        ], _actionToolTip);

        lblMapHint.Visible = canPlot && active;
        lblMapHint.Text = _mapMode switch
        {
            "polygon" => _modeVertexCount < 3
                ? $"Click to add boundary points ({_modeVertexCount}; at least 3). Enter/double-click to finish; Backspace to undo; Esc to cancel."
                : $"Click to add boundary points ({_modeVertexCount}). Enter/double-click to finish; Backspace to undo; Esc to cancel.",
            "editGeometry" => "Drag the vertices. Enter or Finish to save; Esc to cancel.",
            _ => string.Empty
        };
    }

    private void StartDraw()
    {
        if (_presenter is null || _mapMode != "none" || _savingGeometry)
            return;
        var permission = _presenter.PlotPermission;
        if (!permission.Ok)
        {
            ShowActionMessage(permission.Message);
            return;
        }
        try
        {
            _map.StartDraw();
        }
        catch (Exception ex)
        {
            ResetMapModeUi();
            ShowActionMessage($"Parcel drawing could not be started. {ex.Message}");
        }
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
        try
        {
            _map.StartEditGeometry(parcelId);
        }
        catch (Exception ex)
        {
            ResetMapModeUi();
            ShowActionMessage($"Parcel shape editing could not be started. {ex.Message}");
        }
    }

    private async Task HandleDrawCompleteAsync(ParcelGeometryEventArgs args)
    {
        if (_presenter is null)
            return;

        _savingGeometry = true;
        UpdateMapActions();
        try
        {
            if (args.Kind != "polygon")
            {
                ShowActionMessage("New parcels must be drawn as polygons.");
                return;
            }

            var permission = _presenter.PlotPermission;
            if (!permission.Ok)
            {
                ShowActionMessage(permission.Message);
                return;
            }

            var validation = ParcelGeometry.Validate("polygon", args.Points);
            if (!validation.Ok)
            {
                ShowActionMessage(validation.Message);
                return;
            }

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

            var geoJson = ParcelGeometry.BuildPolygonGeoJson(args.Points);
            var areaSqm = ParcelGeometry.ComputeAreaSqm(args.Points);
            using var dialog = new ParcelDialog(
                ownersResult.Value,
                agents,
                null,
                areaSqm,
                geoJson,
                null,
                null)
            {
                CreateOwnerHandler = _presenter.CreateOwnerAsync
            };
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK || dialog.CreateRequest is not { } request)
                return;

            var result = await _presenter.SaveDrawnParcelAsync(request, "polygon", args.Points);
            ShowServiceResult(result.Ok, result.Message);
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
                ShowActionMessage($"The parcel could not be created. {ex.Message}");
        }
        finally
        {
            _savingGeometry = false;
            ResetMapModeUi();
        }
    }

    private async Task HandleGeometryEditCompleteAsync(ParcelGeometryEventArgs args)
    {
        if (_presenter is null || args.ParcelId is not { } parcelId)
            return;

        _savingGeometry = true;
        UpdateMapActions();
        try
        {
            var validation = ParcelGeometry.Validate(args.Kind, args.Points);
            if (!validation.Ok)
            {
                ShowActionMessage(validation.Message);
                return;
            }

            var result = await _presenter.SaveEditedGeometryAsync(parcelId, args.Kind, args.Points);
            ShowServiceResult(result.Ok, result.Message);
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
                ShowActionMessage($"The parcel shape could not be saved. {ex.Message}");
        }
        finally
        {
            _savingGeometry = false;
            ResetMapModeUi();
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
            ShowTransientStatus(message);
        else
            ShowActionMessage(message);
    }

    private void ShowTransientStatus(string message)
    {
        if (IsDisposed)
            return;

        lblMapStatus.Text = message;
        lblMapStatus.Visible = true;
        transientStatusTimer.Stop();
        transientStatusTimer.Start();
    }

    private void ClearTransientStatus()
    {
        if (IsDisposed)
            return;

        transientStatusTimer.Stop();
        lblMapStatus.Text = string.Empty;
        lblMapStatus.Visible = false;
    }

    private void ResetMapModeUi()
    {
        if (IsDisposed)
            return;

        _mapMode = "none";
        _modeVertexCount = 0;
        UpdateMapActions();
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
