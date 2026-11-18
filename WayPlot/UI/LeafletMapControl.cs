using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using WayPlot.domain.Services.DataCollection;

namespace WayPlot.UI;

public partial class LeafletMapControl : UserControl, IParcelMapControl
{
    private const string VirtualHostName = "wayplot.local";
    private const string MapPagePath = "/map/index.html";

    private readonly HashSet<Guid> _knownParcelIds = [];
    private MapTileOptions _tileOptions = MapTileOptions.Default;
    private HashSet<string> _allowedTileOrigins = [];
    private string _featureCollectionJson = "{\"type\":\"FeatureCollection\",\"features\":[]}";
    private Guid? _selectedParcelId;
    private string? _note;
    private string? _message;
    private object? _pendingModeCommand;
    private CoreWebView2? _core;
    private bool _pageReady;
    private bool _fitWhenReady;
    private Guid? _parcelToFitWhenReady;

    public LeafletMapControl()
    {
        InitializeComponent();
        ButtonStyle.Apply(btnFitAll, primary: false);
        btnFitAll.Click += (_, _) => FitAll();
    }

    public event EventHandler<ParcelMapClickEventArgs>? ParcelClicked;
    public event EventHandler? MapBackgroundClicked;
    public event EventHandler<ParcelGeometryEventArgs>? DrawComplete;
    public event EventHandler<ParcelGeometryEventArgs>? GeometryEditComplete;
    public event EventHandler? ModeCancelled;
    public event EventHandler<ParcelMapModeStateEventArgs>? ModeStateChanged;

    public void Configure(MapTileOptions options)
    {
        _tileOptions = options;
        _allowedTileOrigins = GetAllowedTileOrigins(options.TileUrlTemplate);
        if (_pageReady)
            PostTileConfiguration();
    }

    public void LoadParcels(string featureCollectionJson)
    {
        _featureCollectionJson = featureCollectionJson;
        _knownParcelIds.Clear();

        try
        {
            using var document = JsonDocument.Parse(featureCollectionJson);
            if (document.RootElement.TryGetProperty("features", out var features)
                && features.ValueKind == JsonValueKind.Array)
            {
                foreach (var feature in features.EnumerateArray())
                {
                    if (feature.TryGetProperty("properties", out var properties)
                        && properties.TryGetProperty("id", out var id)
                        && id.ValueKind == JsonValueKind.String
                        && Guid.TryParse(id.GetString(), out var parcelId))
                        _knownParcelIds.Add(parcelId);
                }
            }
        }
        catch (JsonException)
        {
            _featureCollectionJson = "{\"type\":\"FeatureCollection\",\"features\":[]}";
            _message = "Parcel map data was invalid and could not be displayed.";
            _knownParcelIds.Clear();
        }

        if (_selectedParcelId is { } selected && !_knownParcelIds.Contains(selected))
            _selectedParcelId = null;
        PostMapState();
    }

    public void SelectParcel(Guid? parcelId)
    {
        _selectedParcelId = parcelId;
        PostMessage(new { type = "selectParcel", parcelId = parcelId?.ToString("D") });
    }

    public void FitAll()
    {
        if (!_pageReady)
        {
            _fitWhenReady = true;
            _parcelToFitWhenReady = null;
            return;
        }

        PostMessage(new { type = "fitAll" });
    }

    public void FitParcel(Guid parcelId)
    {
        if (!_pageReady)
        {
            _fitWhenReady = false;
            _parcelToFitWhenReady = parcelId;
            return;
        }

        _fitWhenReady = false;
        _parcelToFitWhenReady = null;
        PostMessage(new { type = "focusParcel", parcelId = parcelId.ToString("D") });
    }

    public void SetNote(string? text)
    {
        _note = text;
        PostMessage(new { type = "note", text });
    }

    public void SetMessage(string? text)
    {
        _message = text;
        PostMessage(new { type = "message", text });
    }

    public void StartDraw() => PostModeCommand(new { type = "startDraw" });

    public void StartEditGeometry(Guid parcelId)
    {
        if (!_knownParcelIds.Contains(parcelId))
            throw new ArgumentOutOfRangeException(nameof(parcelId), "The parcel is not on the current map.");
        PostModeCommand(new { type = "startEditGeometry", parcelId = parcelId.ToString("D") });
    }

    public void UndoVertex() => PostModeCommand(new { type = "undoVertex" });

    public void FinishMode() => PostModeCommand(new { type = "finishMode" });

    public void CancelMode() => PostModeCommand(new { type = "cancelMode" });

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (DesignMode)
            return;

        try
        {
            await webView.EnsureCoreWebView2Async();
            ConfigureCoreWebView();
        }
        catch (WebView2RuntimeNotFoundException)
        {
            ShowUnavailable("The parcel map needs Microsoft Edge WebView2 Runtime. Install the Evergreen WebView2 Runtime from microsoft.com/edge/webview2, then restart WayPlot.");
        }
        catch (COMException ex)
        {
            ShowUnavailable($"The parcel map could not start WebView2. Install or repair the Microsoft Edge WebView2 Runtime. {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            ShowUnavailable($"The parcel map could not access its browser profile. {ex.Message}");
        }
        catch (IOException ex)
        {
            ShowUnavailable($"The parcel map files could not be loaded. {ex.Message}");
        }
    }

    private void ConfigureCoreWebView()
    {
        _core = webView.CoreWebView2;
        _core.Settings.AreDefaultContextMenusEnabled = false;
        _core.Settings.IsStatusBarEnabled = false;
#if !DEBUG
        _core.Settings.AreDevToolsEnabled = false;
#endif
        _core.SetVirtualHostNameToFolderMapping(
            VirtualHostName,
            Path.Combine(AppContext.BaseDirectory, "assets"),
            CoreWebView2HostResourceAccessKind.DenyCors);

        _core.AddWebResourceRequestedFilter(
            "*",
            CoreWebView2WebResourceContext.All,
            CoreWebView2WebResourceRequestSourceKinds.All);
        _core.WebResourceRequested += HandleWebResourceRequested;
        _core.NavigationStarting += (_, args) =>
        {
            if (!IsAllowedNavigation(args.Uri))
                args.Cancel = true;
        };
        _core.FrameNavigationStarting += (_, args) => args.Cancel = true;
        _core.NewWindowRequested += (_, args) => args.Handled = true;
        _core.PermissionRequested += (_, args) => args.State = CoreWebView2PermissionState.Deny;
        _core.WebMessageReceived += HandleWebMessageReceived;
        _core.ProcessFailed += (_, _) =>
            ShowUnavailable("The parcel map renderer stopped. Close and reopen the Parcel map tab.");

        webView.Source = new Uri($"https://{VirtualHostName}{MapPagePath}");
    }

    private void HandleWebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs args)
    {
        if (IsAllowedResource(args.Request.Uri))
            return;

        args.Response = _core!.Environment.CreateWebResourceResponse(
            new MemoryStream(),
            403,
            "Blocked by WayPlot",
            "Content-Type: text/plain");
    }

    private void HandleWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        if (!IsMapPage(args.Source))
            return;

        try
        {
            using var document = JsonDocument.Parse(args.WebMessageAsJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("type", out var typeValue)
                || typeValue.ValueKind != JsonValueKind.String)
                return;

            switch (typeValue.GetString())
            {
                case "ready":
                    _pageReady = true;
                    PostTileConfiguration();
                    PostMapState();
                    if (_pendingModeCommand is { } pendingModeCommand)
                    {
                        _pendingModeCommand = null;
                        PostMessage(pendingModeCommand);
                    }
                    if (_parcelToFitWhenReady is { } parcelToFit)
                    {
                        _parcelToFitWhenReady = null;
                        FitParcel(parcelToFit);
                    }
                    else if (_fitWhenReady)
                    {
                        _fitWhenReady = false;
                        FitAll();
                    }
                    break;
                case "parcelClicked":
                    if (document.RootElement.TryGetProperty("parcelId", out var idValue)
                        && idValue.ValueKind == JsonValueKind.String
                        && Guid.TryParse(idValue.GetString(), out var parcelId)
                        && _knownParcelIds.Contains(parcelId))
                    {
                        _selectedParcelId = parcelId;
                        ParcelClicked?.Invoke(this, new ParcelMapClickEventArgs(parcelId));
                    }
                    break;
                case "mapBackgroundClicked":
                    _selectedParcelId = null;
                    MapBackgroundClicked?.Invoke(this, EventArgs.Empty);
                    break;
                case "drawComplete":
                    if (TryReadGeometry(document.RootElement, allowMarker: false, out var drawKind, out var drawPoints))
                        DrawComplete?.Invoke(this, new ParcelGeometryEventArgs(drawKind, drawPoints));
                    break;
                case "geometryEditComplete":
                    if (TryReadGeometry(document.RootElement, allowMarker: true, out var editKind, out var editPoints)
                        && document.RootElement.TryGetProperty("parcelId", out var editIdValue)
                        && editIdValue.ValueKind == JsonValueKind.String
                        && Guid.TryParse(editIdValue.GetString(), out var editParcelId)
                        && _selectedParcelId == editParcelId
                        && _knownParcelIds.Contains(editParcelId))
                    {
                        GeometryEditComplete?.Invoke(
                            this,
                            new ParcelGeometryEventArgs(editKind, editPoints, editParcelId));
                    }
                    break;
                case "modeCancelled":
                    ModeCancelled?.Invoke(this, EventArgs.Empty);
                    break;
                case "modeState":
                    if (document.RootElement.TryGetProperty("mode", out var modeValue)
                        && modeValue.ValueKind == JsonValueKind.String
                        && modeValue.GetString() is { } mode
                        && mode is "none" or "polygon" or "editGeometry"
                        && document.RootElement.TryGetProperty("vertexCount", out var countValue)
                        && countValue.TryGetInt32(out var vertexCount)
                        && vertexCount >= 0)
                    {
                        ModeStateChanged?.Invoke(this, new ParcelMapModeStateEventArgs(mode, vertexCount));
                    }
                    break;
            }
        }
        catch (JsonException)
        {
            // Malformed or unexpected web messages are ignored.
        }
    }

    private static bool TryReadGeometry(
        JsonElement message,
        bool allowMarker,
        out string kind,
        out IReadOnlyList<ParcelCoordinate> points)
    {
        kind = string.Empty;
        points = [];
        if (!message.TryGetProperty("kind", out var kindValue)
            || kindValue.ValueKind != JsonValueKind.String
            || kindValue.GetString() is not ("polygon" or "marker")
            || (!allowMarker && kindValue.GetString() != "polygon")
            || !message.TryGetProperty("points", out var pointsValue)
            || pointsValue.ValueKind != JsonValueKind.Array)
            return false;

        var parsedPoints = new List<ParcelCoordinate>();
        foreach (var point in pointsValue.EnumerateArray())
        {
            if (point.ValueKind != JsonValueKind.Array
                || point.GetArrayLength() != 2
                || point[0].ValueKind != JsonValueKind.Number
                || point[1].ValueKind != JsonValueKind.Number
                || !point[0].TryGetDouble(out var longitude)
                || !point[1].TryGetDouble(out var latitude))
                return false;
            parsedPoints.Add(new ParcelCoordinate(longitude, latitude));
        }

        kind = kindValue.GetString()!;
        points = parsedPoints;
        return true;
    }

    private void PostModeCommand<T>(T message)
    {
        if (!_pageReady)
        {
            _pendingModeCommand = message;
            return;
        }

        PostMessage(message);
    }

    private bool IsAllowedResource(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            return false;
        if (string.Equals(uri.Host, VirtualHostName, StringComparison.OrdinalIgnoreCase))
            return true;
        return _allowedTileOrigins.Contains(uri.GetLeftPart(UriPartial.Authority));
    }

    private bool IsAllowedNavigation(string value)
    {
        if (IsMapPage(value))
            return true;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            return false;
        return _allowedTileOrigins.Contains(uri.GetLeftPart(UriPartial.Authority));
    }

    private static bool IsMapPage(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && string.Equals(uri.Host, VirtualHostName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(uri.AbsolutePath, MapPagePath, StringComparison.Ordinal);

    private static HashSet<string> GetAllowedTileOrigins(string template)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(template))
            return result;

        var subdomains = template.Contains("{s}", StringComparison.Ordinal) ? new[] { "a", "b", "c" } : [string.Empty];
        foreach (var subdomain in subdomains)
        {
            var sample = template
                .Replace("{s}", subdomain, StringComparison.Ordinal)
                .Replace("{z}", "0", StringComparison.Ordinal)
                .Replace("{x}", "0", StringComparison.Ordinal)
                .Replace("{y}", "0", StringComparison.Ordinal)
                .Replace("{r}", string.Empty, StringComparison.Ordinal);
            if (Uri.TryCreate(sample, UriKind.Absolute, out var uri)
                && uri.Scheme == Uri.UriSchemeHttps)
                result.Add(uri.GetLeftPart(UriPartial.Authority));
        }
        return result;
    }

    private void PostTileConfiguration()
    {
        PostMessage(new
        {
            type = "configureTiles",
            tileUrlTemplate = _allowedTileOrigins.Count == 0 ? string.Empty : _tileOptions.TileUrlTemplate,
            attribution = _tileOptions.Attribution
        });
    }

    private void PostMapState()
    {
        PostMessage(new { type = "loadParcels", featureCollectionJson = _featureCollectionJson });
        PostMessage(new { type = "note", text = _note });
        PostMessage(new { type = "message", text = _message });
        PostMessage(new { type = "selectParcel", parcelId = _selectedParcelId?.ToString("D") });
    }

    private void PostMessage<T>(T message)
    {
        if (_pageReady && _core is not null)
            _core.PostWebMessageAsJson(JsonSerializer.Serialize(message));
    }

    private void ShowUnavailable(string message)
    {
        if (IsDisposed)
            return;
        webView.Visible = false;
        btnFitAll.Visible = false;
        lblUnavailable.Text = message;
        lblUnavailable.Visible = true;
    }
}
