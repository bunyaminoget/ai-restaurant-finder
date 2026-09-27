namespace NearbyEats.Application.Restaurants;

public sealed class GetRestaurantDetailHandler
{
    private readonly IRestaurantDetailsProvider _provider;

    public GetRestaurantDetailHandler(IRestaurantDetailsProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
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

        var restaurant = await _provider.GetByIdAsync(query.Id, cancellationToken).ConfigureAwait(false);

        if (restaurant is null)
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
