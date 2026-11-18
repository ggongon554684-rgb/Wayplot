namespace WayPlot.UI;

public sealed record MapTileOptions(string TileUrlTemplate, string Attribution)
{
    // The public OpenStreetMap tile servers are for development; production deployments need a tile provider.
    public static MapTileOptions Default { get; } = new(
        "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
        "© OpenStreetMap contributors");
}
