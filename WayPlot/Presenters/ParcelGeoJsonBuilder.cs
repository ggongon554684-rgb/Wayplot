using System.Text.Json;
using WayPlot.domain.Contracts;

namespace WayPlot.Presenters;

public sealed record ParcelGeoJsonResult(string FeatureCollection, int FeatureCount, int Unplotted);

/// <summary>Builds a GeoJSON FeatureCollection from the already-authorized parcel DTOs.</summary>
public static class ParcelGeoJsonBuilder
{
    public static ParcelGeoJsonResult Build(IReadOnlyList<ParcelDto> parcels)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("type", "FeatureCollection");
            writer.WriteStartArray("features");

            var featureCount = 0;
            var unplotted = 0;
            foreach (var parcel in parcels)
            {
                var geometry = GetPolygon(parcel);
                var marker = geometry is null ? GetMarker(parcel) : null;
                if (geometry is null && marker is null)
                {
                    unplotted++;
                    continue;
                }

                writer.WriteStartObject();
                writer.WriteString("type", "Feature");
                writer.WritePropertyName("geometry");
                if (geometry is { } polygon)
                {
                    polygon.WriteTo(writer);
                }
                else
                {
                    writer.WriteStartObject();
                    writer.WriteString("type", "Point");
                    writer.WriteStartArray("coordinates");
                    writer.WriteNumberValue(marker!.Value.Longitude);
                    writer.WriteNumberValue(marker.Value.Latitude);
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }

                writer.WriteStartObject("properties");
                writer.WriteString("id", parcel.Id);
                writer.WriteString("label", parcel.Label);
                writer.WriteString("status", parcel.Status.ToString());
                writer.WriteNumber("price", parcel.Price);
                writer.WriteEndObject();
                writer.WriteEndObject();
                featureCount++;
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.Flush();
            return new ParcelGeoJsonResult(
                System.Text.Encoding.UTF8.GetString(stream.ToArray()),
                featureCount,
                unplotted);
        }
    }

    private static JsonElement? GetPolygon(ParcelDto parcel)
    {
        if (string.IsNullOrWhiteSpace(parcel.GeoJson))
            return null;

        try
        {
            using var document = JsonDocument.Parse(parcel.GeoJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("type", out var type)
                || type.ValueKind != JsonValueKind.String
                || type.GetString() != "Polygon"
                || !root.TryGetProperty("coordinates", out var coordinates)
                || !IsValidPolygonCoordinates(coordinates))
                return null;

            return root.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool IsValidPolygonCoordinates(JsonElement coordinates)
    {
        if (coordinates.ValueKind != JsonValueKind.Array || coordinates.GetArrayLength() == 0)
            return false;

        foreach (var ring in coordinates.EnumerateArray())
        {
            if (ring.ValueKind != JsonValueKind.Array || ring.GetArrayLength() < 4)
                return false;

            foreach (var position in ring.EnumerateArray())
            {
                if (position.ValueKind != JsonValueKind.Array || position.GetArrayLength() < 2)
                    return false;
                var longitude = position[0];
                var latitude = position[1];
                if (longitude.ValueKind != JsonValueKind.Number
                    || latitude.ValueKind != JsonValueKind.Number
                    || !longitude.TryGetDouble(out var lon)
                    || !latitude.TryGetDouble(out var lat)
                    || !double.IsFinite(lon)
                    || !double.IsFinite(lat)
                    || lon is < -180 or > 180
                    || lat is < -90 or > 90)
                    return false;
            }
        }

        return true;
    }

    private static (double Longitude, double Latitude)? GetMarker(ParcelDto parcel)
    {
        if (parcel.Latitude is not { } latitude || parcel.Longitude is not { } longitude)
            return null;
        if (!double.IsFinite(latitude) || !double.IsFinite(longitude)
            || latitude is < -90 or > 90 || longitude is < -180 or > 180)
            return null;
        return (longitude, latitude);
    }
}
