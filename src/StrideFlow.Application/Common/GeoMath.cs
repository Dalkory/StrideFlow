namespace StrideFlow.Application.Common;

public static class GeoMath
{
    private const double EarthRadiusMeters = 6_371_000d;

    public static double HaversineDistanceMeters(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        var lat1 = DegreesToRadians(latitude1);
        var lon1 = DegreesToRadians(longitude1);
        var lat2 = DegreesToRadians(latitude2);
        var lon2 = DegreesToRadians(longitude2);

        var dLat = lat2 - lat1;
        var dLon = lon2 - lon1;

        var a = Math.Pow(Math.Sin(dLat / 2d), 2d)
                + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLon / 2d), 2d);

        var c = 2d * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1d - a));
        return EarthRadiusMeters * c;
    }

    public static int EstimateSteps(double totalDistanceMeters, double stepLengthMeters)
    {
        if (stepLengthMeters <= 0d)
        {
            return 0;
        }

        return Math.Max(0, (int)Math.Round(totalDistanceMeters / stepLengthMeters, MidpointRounding.AwayFromZero));
    }

    public static double EstimateCalories(double distanceMeters, double weightKg)
    {
        if (distanceMeters <= 0d || weightKg <= 0d)
        {
            return 0d;
        }

        var distanceKm = distanceMeters / 1_000d;
        return Math.Round(distanceKm * weightKg * 1.036d, 2, MidpointRounding.AwayFromZero);
    }

    public static double EstimateSpeedMetersPerSecond(double distanceMeters, DateTimeOffset previousAt, DateTimeOffset currentAt)
    {
        var seconds = (currentAt - previousAt).TotalSeconds;
        if (seconds <= 0d)
        {
            return 0d;
        }

        return distanceMeters / seconds;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;
}
