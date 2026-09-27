using NearbyEats.Domain.Restaurants;

namespace NearbyEats.Application.Restaurants;

public sealed class SearchNearbyRestaurantsHandler
{
    private readonly IRestaurantSearchProvider _provider;

    public SearchNearbyRestaurantsHandler(IRestaurantSearchProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _provider = provider;
    }

    public async Task<NearbyRestaurantsResult> HandleAsync(
        SearchNearbyRestaurantsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 1 : query.PageSize > 20 ? 20 : query.PageSize;

        var center = new GeoLocation(query.Latitude, query.Longitude);

        var restaurants = await _provider.SearchNearbyAsync(center, cancellationToken).ConfigureAwait(false);

        ArgumentNullException.ThrowIfNull(restaurants);

        var candidates = restaurants
            .Select(r => new NearbyRestaurantDto(
                r.Id,
                r.Name,
                r.Location.Latitude,
                r.Location.Longitude,
                r.Rating,
                r.ReviewCount,
                center.DistanceKmTo(r.Location)))
            .Where(dto => dto.DistanceKm <= query.RadiusKm);

        if (query.MinRating.HasValue)
        {
            candidates = candidates.Where(dto => dto.Rating >= query.MinRating.Value);
        }

        candidates = SortCandidates(candidates, query.SortBy, query.SortDirection);

        var sorted = candidates.ToList();

        var totalCount = sorted.Count;
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        var items = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var hasNextPage = page * pageSize < totalCount;

        return new NearbyRestaurantsResult(items, page, pageSize, totalCount, totalPages, hasNextPage);
    }

    private static IEnumerable<NearbyRestaurantDto> SortCandidates(
        IEnumerable<NearbyRestaurantDto> candidates,
        RestaurantSortBy sortBy,
        SortDirection sortDirection)
    {
        var ascending = sortDirection == SortDirection.Asc;

        return sortBy switch
        {
            RestaurantSortBy.Distance => ascending
                ? candidates
                    .OrderBy(dto => dto.DistanceKm)
                    .ThenByDescending(dto => dto.Rating)
                    .ThenByDescending(dto => dto.ReviewCount)
                : candidates
                    .OrderByDescending(dto => dto.DistanceKm)
                    .ThenByDescending(dto => dto.Rating)
                    .ThenByDescending(dto => dto.ReviewCount),
            RestaurantSortBy.ReviewCount => ascending
                ? candidates
                    .OrderBy(dto => dto.ReviewCount)
                    .ThenByDescending(dto => dto.Rating)
                    .ThenBy(dto => dto.DistanceKm)
                : candidates
                    .OrderByDescending(dto => dto.ReviewCount)
                    .ThenByDescending(dto => dto.Rating)
                    .ThenBy(dto => dto.DistanceKm),
            _ => ascending
                ? candidates
                    .OrderBy(dto => dto.Rating)
                    .ThenByDescending(dto => dto.ReviewCount)
                    .ThenBy(dto => dto.DistanceKm)
                : candidates
                    .OrderByDescending(dto => dto.Rating)
                    .ThenByDescending(dto => dto.ReviewCount)
                    .ThenBy(dto => dto.DistanceKm),
        };
    }
}
