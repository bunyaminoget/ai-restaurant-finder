namespace NearbyEats.Application.Restaurants;

public sealed class GetRestaurantDetailHandler
{
    private readonly IRestaurantStore _store;
    private readonly IRestaurantDetailsProvider _provider;

    public GetRestaurantDetailHandler(IRestaurantStore store, IRestaurantDetailsProvider provider)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(provider);
        _store = store;
        _provider = provider;
    }

    public async Task<RestaurantDetailDto?> HandleAsync(
        GetRestaurantDetailQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Id == Guid.Empty)
        {
            throw new ArgumentException("Id must not be empty.", nameof(query));
        }

        // Resolve the public id to a Google place id through PostgreSQL first
        // (no process-local registry), then fetch fresh details from Google.
        var stored = await _store.GetByIdAsync(query.Id, cancellationToken).ConfigureAwait(false);

        if (stored is null || string.IsNullOrWhiteSpace(stored.GooglePlaceId))
        {
            return null;
        }

        var restaurant = await _provider.GetByPlaceIdAsync(stored.GooglePlaceId, cancellationToken).ConfigureAwait(false);

        if (restaurant is null || restaurant.Id != query.Id)
        {
            return null;
        }

        return new RestaurantDetailDto(
            restaurant.Id,
            restaurant.Name,
            restaurant.Location.Latitude,
            restaurant.Location.Longitude,
            restaurant.Rating,
            restaurant.ReviewCount);
    }
}
