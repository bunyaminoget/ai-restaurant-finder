namespace NearbyEats.Infrastructure.Restaurants;

public sealed class GooglePlacesOptions
{
    public const string SectionName = "GooglePlaces";

    public string ApiKey { get; set; } = string.Empty;
}
