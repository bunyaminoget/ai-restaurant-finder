namespace NearbyEats.Application.Restaurants;

public sealed record NearbyRestaurantDto(
    Guid Id,
    string Name,
    double Latitude,
    double Longitude,
    double Rating,
    int ReviewCount,
    double DistanceKm);
