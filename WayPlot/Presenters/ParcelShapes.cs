using System.Text.Json;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;

namespace WayPlot.Presenters;

/// <summary>A point in map space (Web Mercator radians). Double precision, because a 100 m lot is a tiny number of radians.</summary>
public readonly record struct PointD(double X, double Y);

/// <summary>What the map draws for one parcel: a polygon (one or more rings) or a single marker.
/// Rings are filled with the even-odd rule, so a ring inside another ring is a hole.</summary>
public sealed class MapShape
{
    public MapShape(Guid id, string label, ParcelStatus status, IReadOnlyList<PointD[]> rings, PointD? marker)
    {
        Id = id;
        Label = label;
        Status = status;
        Rings = rings;
        Marker = marker;

        var minX = double.MaxValue; var minY = double.MaxValue;
        var maxX = double.MinValue; var maxY = double.MinValue;
        void Take(PointD p)
        {
            if (p.X < minX) minX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.X > maxX) maxX = p.X;
            if (p.Y > maxY) maxY = p.Y;
        }
        foreach (var ring in rings)
            foreach (var p in ring)
                Take(p);
        if (marker is { } m) Take(m);

        MinX = minX; MinY = minY; MaxX = maxX; MaxY = maxY;
    }

    public Guid Id { get; }
    public string Label { get; }
    public ParcelStatus Status { get; }
    public IReadOnlyList<PointD[]> Rings { get; }
    public PointD? Marker { get; }
    public double MinX { get; }
    public double MinY { get; }
    public double MaxX { get; }
    public double MaxY { get; }

    /// <summary>True when the point is inside the polygon. Markers are hit-tested on screen, so they return false here.</summary>
    public bool Contains(PointD p)
    {
        if (Marker is not null) return false;
        if (p.X < MinX || p.X > MaxX || p.Y < MinY || p.Y > MaxY) return false;

        var inside = false;
        foreach (var ring in Rings)
        {
            for (int i = 0, j = ring.Length - 1; i < ring.Length; j = i++)
            {
                var a = ring[i];
                var b = ring[j];
                if ((a.Y > p.Y) != (b.Y > p.Y) &&
                    p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X)
                    inside = !inside;
            }
        }
        return inside;
    }
}

/// <summary>Turns a ParcelDto into a MapShape. Reads GeoJSON (Polygon, MultiPolygon, Point, Feature, FeatureCollection,
/// GeometryCollection; coordinates are [longitude, latitude]). A parcel with no boundary falls back to its Latitude/Longitude marker.
/// Returns null when the parcel has no usable position.</summary>
public static class ParcelShapeBuilder
{
    public static MapShape? Build(ParcelDto parcel)
    {
        var rings = new List<PointD[]>();
        PointD? marker = null;

        if (!string.IsNullOrWhiteSpace(parcel.GeoJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(parcel.GeoJson);
                Collect(doc.RootElement, rings, ref marker);
            }
            catch (JsonException)
            {
                // Bad boundary text: fall through to the marker, or to "no position".
            }
        }

        if (rings.Count == 0 && marker is null &&
            parcel.Latitude is { } lat && parcel.Longitude is { } lon &&
            TryProject(lon, lat, out var projected))
            marker = projected;

        if (rings.Count == 0 && marker is null)
            return null;

        // A polygon wins over a marker when both exist.
        return new MapShape(parcel.Id, parcel.Label, parcel.Status, rings, rings.Count > 0 ? null : marker);
    }

    private static void Collect(JsonElement e, List<PointD[]> rings, ref PointD? marker)
    {
        if (e.ValueKind != JsonValueKind.Object ||
            !e.TryGetProperty("type", out var typeProp) ||
            typeProp.ValueKind != JsonValueKind.String)
            return;

        switch (typeProp.GetString())
        {
            case "FeatureCollection":
                if (e.TryGetProperty("features", out var features) && features.ValueKind == JsonValueKind.Array)
                    foreach (var feature in features.EnumerateArray())
                        Collect(feature, rings, ref marker);
                break;

            case "Feature":
                if (e.TryGetProperty("geometry", out var geometry))
                    Collect(geometry, rings, ref marker);
                break;

            case "GeometryCollection":
                if (e.TryGetProperty("geometries", out var geometries) && geometries.ValueKind == JsonValueKind.Array)
                    foreach (var item in geometries.EnumerateArray())
                        Collect(item, rings, ref marker);
                break;

            case "Polygon":
                if (e.TryGetProperty("coordinates", out var polygonCoords))
                    AddPolygon(polygonCoords, rings);
                break;

            case "MultiPolygon":
                if (e.TryGetProperty("coordinates", out var multiCoords) && multiCoords.ValueKind == JsonValueKind.Array)
                    foreach (var polygon in multiCoords.EnumerateArray())
                        AddPolygon(polygon, rings);
                break;

            case "Point":
                if (e.TryGetProperty("coordinates", out var pointCoords) && TryPosition(pointCoords, out var point))
                    marker ??= point;
                break;
        }
    }

    private static void AddPolygon(JsonElement polygon, List<PointD[]> rings)
    {
        if (polygon.ValueKind != JsonValueKind.Array) return;

        foreach (var ringElement in polygon.EnumerateArray())
        {
            if (ringElement.ValueKind != JsonValueKind.Array) continue;

            var points = new List<PointD>();
            foreach (var position in ringElement.EnumerateArray())
                if (TryPosition(position, out var p))
                    points.Add(p);

            // GeoJSON repeats the first point at the end. Drop it; the path closes itself.
            if (points.Count > 1 && points[0] == points[^1])
                points.RemoveAt(points.Count - 1);

            if (points.Count >= 3)
                rings.Add(points.ToArray());
        }
    }

    private static bool TryPosition(JsonElement position, out PointD point)
    {
        point = default;
        if (position.ValueKind != JsonValueKind.Array || position.GetArrayLength() < 2) return false;

        var first = position[0];
        var second = position[1];
        if (first.ValueKind != JsonValueKind.Number || second.ValueKind != JsonValueKind.Number) return false;
        if (!first.TryGetDouble(out var lon) || !second.TryGetDouble(out var lat)) return false;

        return TryProject(lon, lat, out point);
    }

    /// <summary>Longitude/latitude in degrees to Web Mercator radians (the same projection OpenStreetMap tiles use later).</summary>
    private static bool TryProject(double lon, double lat, out PointD point)
    {
        point = default;

        // Safety net for a [latitude, longitude] pair: in the Philippines longitude is above 90, latitude never is.
        if (Math.Abs(lat) > 90 && Math.Abs(lon) <= 90)
            (lon, lat) = (lat, lon);

        if (double.IsNaN(lon) || double.IsNaN(lat) || Math.Abs(lat) > 85 || Math.Abs(lon) > 180)
            return false;

        var x = lon * Math.PI / 180.0;
        var y = Math.Log(Math.Tan(Math.PI / 4.0 + lat * Math.PI / 360.0));
        point = new PointD(x, y);
        return true;
    }
}