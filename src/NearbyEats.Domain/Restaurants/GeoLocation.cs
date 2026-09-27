namespace NearbyEats.Domain.Restaurants;

public sealed class GeoLocation
{
    private const double EarthRadiusKm = 6371.0;

    public double Latitude { get; }
    public double Longitude { get; }

    public GeoLocation(double latitude, double longitude)
    {
        if (latitude < -90 || latitude > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "Latitude must be between -90 and 90.");
        }

        if (longitude < -180 || longitude > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Longitude must be between -180 and 180.");
        }

        Latitude = latitude;
        Longitude = longitude;
    }

    public double DistanceKmTo(GeoLocation other)
    {
        ArgumentNullException.ThrowIfNull(other);

        var lat1Rad = Latitude * Math.PI / 180.0;
        var lat2Rad = other.Latitude * Math.PI / 180.0;
        var dLatRad = (other.Latitude - Latitude) * Math.PI / 180.0;
        var dLonRad = (other.Longitude - Longitude) * Math.PI / 180.0;

        var a = Math.Sin(dLatRad / 2.0) * Math.Sin(dLatRad / 2.0) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(dLonRad / 2.0) * Math.Sin(dLonRad / 2.0);
        var c = 2.0 * Math.Asin(Math.Sqrt(a));

        return EarthRadiusKm * c;
    }
}
