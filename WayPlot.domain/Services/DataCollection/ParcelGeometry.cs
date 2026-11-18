using System.Text.Json;
using WayPlot.domain.Common;

namespace WayPlot.domain.Services.DataCollection;

public readonly record struct ParcelCoordinate(double Longitude, double Latitude);

public static class ParcelGeometry
{
    private const double EarthRadiusMeters = 6_378_137d;
    private const double Epsilon = 1e-12;

    public static RuleResult Validate(string kind, IReadOnlyList<ParcelCoordinate> points)
    {
        if (kind == "marker")
        {
            if (points.Count != 1)
                return RuleResult.Fail("A marker must have exactly one point.");
            return IsValidCoordinate(points[0])
                ? RuleResult.Success()
                : RuleResult.Fail("The marker longitude or latitude is out of range.");
        }

        if (kind != "polygon")
            return RuleResult.Fail("Choose a polygon or marker geometry.");
        if (points.Count < 3)
            return RuleResult.Fail("A parcel boundary needs at least 3 distinct points.");
        if (points.Count > 500)
            return RuleResult.Fail("A parcel boundary can have at most 500 points.");
        var distinctPointCount = points.Distinct().Count();
        if (distinctPointCount < 3)
            return RuleResult.Fail("A parcel boundary needs at least 3 distinct points.");
        if (distinctPointCount != points.Count)
            return RuleResult.Fail("A parcel boundary cannot repeat a vertex.");
        if (points.Any(point => !IsValidCoordinate(point)))
            return RuleResult.Fail("A parcel boundary contains a longitude or latitude out of range.");

        var longitudes = points.Select(point => point.Longitude);
        if (longitudes.Max() - longitudes.Min() > 180)
            return RuleResult.Fail("A parcel boundary cannot span more than 180 degrees of longitude.");
        if (SelfIntersects(points))
            return RuleResult.Fail("The parcel boundary crosses itself. Redraw it without crossing edges.");

        return RuleResult.Success();
    }

    public static string BuildPolygonGeoJson(IReadOnlyList<ParcelCoordinate> points)
    {
        var validation = Validate("polygon", points);
        if (!validation.Ok)
            throw new ArgumentException(validation.Message, nameof(points));

        var ring = new double[points.Count + 1][];
        for (var index = 0; index < points.Count; index++)
        {
            ring[index] =
            [
                Math.Round(points[index].Longitude, 7, MidpointRounding.AwayFromZero),
                Math.Round(points[index].Latitude, 7, MidpointRounding.AwayFromZero)
            ];
        }
        ring[^1] = [ring[0][0], ring[0][1]];

        return JsonSerializer.Serialize(new
        {
            type = "Polygon",
            coordinates = new[] { ring }
        });
    }

    public static decimal ComputeAreaSqm(IReadOnlyList<ParcelCoordinate> points)
    {
        var validation = Validate("polygon", points);
        if (!validation.Ok)
            throw new ArgumentException(validation.Message, nameof(points));

        var sum = 0d;
        for (var index = 0; index < points.Count; index++)
        {
            var current = points[index];
            var next = points[(index + 1) % points.Count];
            var longitudeDelta = DegreesToRadians(next.Longitude - current.Longitude);
            var latitude = DegreesToRadians(current.Latitude);
            var nextLatitude = DegreesToRadians(next.Latitude);
            sum += longitudeDelta * (2 + Math.Sin(latitude) + Math.Sin(nextLatitude));
        }

        var area = Math.Abs(sum) * EarthRadiusMeters * EarthRadiusMeters / 2;
        return decimal.Round((decimal)area, 2, MidpointRounding.AwayFromZero);
    }

    public static ParcelCoordinate Centroid(IReadOnlyList<ParcelCoordinate> points)
    {
        if (points.Count == 0)
            throw new ArgumentException("A polygon needs at least one point.", nameof(points));

        var twiceSignedArea = 0d;
        var longitudeSum = 0d;
        var latitudeSum = 0d;
        for (var index = 0; index < points.Count; index++)
        {
            var current = points[index];
            var next = points[(index + 1) % points.Count];
            var cross = current.Longitude * next.Latitude - next.Longitude * current.Latitude;
            twiceSignedArea += cross;
            longitudeSum += (current.Longitude + next.Longitude) * cross;
            latitudeSum += (current.Latitude + next.Latitude) * cross;
        }

        if (Math.Abs(twiceSignedArea) <= Epsilon)
        {
            return new ParcelCoordinate(
                points.Average(point => point.Longitude),
                points.Average(point => point.Latitude));
        }

        return new ParcelCoordinate(
            longitudeSum / (3 * twiceSignedArea),
            latitudeSum / (3 * twiceSignedArea));
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;

    private static bool IsValidCoordinate(ParcelCoordinate point) =>
        double.IsFinite(point.Longitude)
        && double.IsFinite(point.Latitude)
        && point.Longitude is >= -180 and <= 180
        && point.Latitude is >= -90 and <= 90;

    private static bool SelfIntersects(IReadOnlyList<ParcelCoordinate> points)
    {
        for (var first = 0; first < points.Count; first++)
        {
            var firstNext = (first + 1) % points.Count;
            for (var second = first + 1; second < points.Count; second++)
            {
                var secondNext = (second + 1) % points.Count;
                if (first == second || firstNext == second || secondNext == first)
                    continue;

                if (SegmentsIntersect(points[first], points[firstNext], points[second], points[secondNext]))
                    return true;
            }
        }

        return false;
    }

    private static bool SegmentsIntersect(
        ParcelCoordinate a,
        ParcelCoordinate b,
        ParcelCoordinate c,
        ParcelCoordinate d)
    {
        var abC = Orientation(a, b, c);
        var abD = Orientation(a, b, d);
        var cdA = Orientation(c, d, a);
        var cdB = Orientation(c, d, b);

        if (Math.Abs(abC) <= Epsilon && OnSegment(a, c, b)) return true;
        if (Math.Abs(abD) <= Epsilon && OnSegment(a, d, b)) return true;
        if (Math.Abs(cdA) <= Epsilon && OnSegment(c, a, d)) return true;
        if (Math.Abs(cdB) <= Epsilon && OnSegment(c, b, d)) return true;

        return (abC > 0) != (abD > 0) && (cdA > 0) != (cdB > 0);
    }

    private static double Orientation(ParcelCoordinate a, ParcelCoordinate b, ParcelCoordinate c) =>
        (b.Longitude - a.Longitude) * (c.Latitude - a.Latitude)
        - (b.Latitude - a.Latitude) * (c.Longitude - a.Longitude);

    private static bool OnSegment(ParcelCoordinate a, ParcelCoordinate point, ParcelCoordinate b) =>
        point.Longitude >= Math.Min(a.Longitude, b.Longitude) - Epsilon
        && point.Longitude <= Math.Max(a.Longitude, b.Longitude) + Epsilon
        && point.Latitude >= Math.Min(a.Latitude, b.Latitude) - Epsilon
        && point.Latitude <= Math.Max(a.Latitude, b.Latitude) + Epsilon;
}
