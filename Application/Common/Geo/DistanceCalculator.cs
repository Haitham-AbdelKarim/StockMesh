namespace Application.Common.Geo;

public static class DistanceCalculator
{
    private const double EarthRadiusKm = 6371.0;

    public static double HaversineKm(
        double latitude1Degrees,
        double longitude1Degrees,
        double latitude2Degrees,
        double longitude2Degrees)
    {
        var dLat = ToRadians(latitude2Degrees - latitude1Degrees);
        var dLon = ToRadians(longitude2Degrees - longitude1Degrees);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRadians(latitude1Degrees))
                * Math.Cos(ToRadians(latitude2Degrees))
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        return EarthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}