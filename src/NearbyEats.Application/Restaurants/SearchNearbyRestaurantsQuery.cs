namespace NearbyEats.Application.Restaurants;

public sealed record SearchNearbyRestaurantsQuery(
    double Latitude,
    double Longitude,
    double RadiusKm = 2,
    double? MinRating = null,
    RestaurantSortBy SortBy = RestaurantSortBy.Rating,
    SortDirection SortDirection = SortDirection.Desc,
    int Page = 1,
    int PageSize = 20);
