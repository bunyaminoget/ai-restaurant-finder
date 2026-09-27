using NearbyEats.Domain.Restaurants;

namespace NearbyEats.Application.Restaurants;

/// <summary>
/// Minimal persistence surface for restaurants synced from the provider.
/// Implementations live in Infrastructure; handlers use it to persist Google
/// Places search results and to resolve a public <see cref="Guid"/> id to its
/// Google place id for detail lookups.
/// </summary>
public interface IRestaurantStore
{
    Task<Restaurant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Restaurant?> GetByGooglePlaceIdAsync(string googlePlaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts new restaurants and refreshes the Google-provided fields
    /// (name/location/rating/review count) of existing rows matched by
    /// <c>GooglePlaceId</c>. Stored ids are never reassigned, so public ids
    /// stay stable. Returns the persisted instances.
    /// </summary>
    Task<IReadOnlyList<Restaurant>> UpsertAsync(IReadOnlyList<Restaurant> restaurants, CancellationToken cancellationToken = default);
}
