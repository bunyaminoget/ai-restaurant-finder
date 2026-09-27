namespace NearbyEats.Application.Restaurants;

public sealed record NearbyRestaurantsResult(
    IReadOnlyList<NearbyRestaurantDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasNextPage);
