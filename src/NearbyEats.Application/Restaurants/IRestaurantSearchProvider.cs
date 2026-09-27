using NearbyEats.Domain.Restaurants;

namespace NearbyEats.Application.Restaurants;

public interface IRestaurantSearchProvider
{
    Task<IReadOnlyList<Restaurant>> SearchNearbyAsync(GeoLocation center, CancellationToken cancellationToken = default);
}
