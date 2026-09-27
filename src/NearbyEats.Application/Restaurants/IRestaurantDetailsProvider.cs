using NearbyEats.Domain.Restaurants;

namespace NearbyEats.Application.Restaurants;

public interface IRestaurantDetailsProvider
{
    /// <summary>
    /// Fetches fresh place details for a Google place id previously persisted
    /// via <see cref="IRestaurantStore"/>. Callers resolve the public
    /// restaurant id to its place id through the store first, so the provider
    /// itself keeps no process-local id registry.
    /// </summary>
    Task<Restaurant?> GetByPlaceIdAsync(string googlePlaceId, CancellationToken cancellationToken = default);
}
