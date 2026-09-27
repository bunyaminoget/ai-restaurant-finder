using NearbyEats.Application.Restaurants;

namespace NearbyEats.Api.Restaurants;

public sealed record NearbyRestaurantsPaginationResponse(
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasNextPage);

public sealed record NearbyRestaurantsResponse(
    IReadOnlyList<NearbyRestaurantDto> Items,
    NearbyRestaurantsPaginationResponse Pagination);
