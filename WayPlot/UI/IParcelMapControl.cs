using WayPlot.domain.Services.DataCollection;

namespace WayPlot.UI;

public sealed class ParcelMapClickEventArgs(Guid parcelId) : EventArgs
{
    public Guid ParcelId { get; } = parcelId;
}

public sealed class ParcelGeometryEventArgs(
    string kind,
    IReadOnlyList<ParcelCoordinate> points,
    Guid? parcelId = null) : EventArgs
{
    public string Kind { get; } = kind;
    public IReadOnlyList<ParcelCoordinate> Points { get; } = points;
    public Guid? ParcelId { get; } = parcelId;
}

public sealed class ParcelMapModeStateEventArgs(string mode, int vertexCount) : EventArgs
{
    public string Mode { get; } = mode;
    public int VertexCount { get; } = vertexCount;
}

public interface IParcelMapControl
{
    event EventHandler<ParcelMapClickEventArgs>? ParcelClicked;
    event EventHandler? MapBackgroundClicked;
    event EventHandler<ParcelGeometryEventArgs>? DrawComplete;
    event EventHandler<ParcelGeometryEventArgs>? GeometryEditComplete;
    event EventHandler? ModeCancelled;
    event EventHandler<ParcelMapModeStateEventArgs>? ModeStateChanged;

    void Configure(MapTileOptions options);
    void LoadParcels(string featureCollectionJson);
    void SelectParcel(Guid? parcelId);
    void FitAll();
    void FitParcel(Guid parcelId);
    void SetNote(string? text);
    void SetMessage(string? text);
    void StartDraw();
    void StartEditGeometry(Guid parcelId);
    void UndoVertex();
    void FinishMode();
    void CancelMode();
}
