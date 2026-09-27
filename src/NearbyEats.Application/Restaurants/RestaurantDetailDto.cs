namespace NearbyEats.Application.Restaurants;

public sealed record RestaurantDetailDto(
    Guid Id,
    string Name,
    double Latitude,
    double Longitude,
    double Rating,
    int ReviewCount);
