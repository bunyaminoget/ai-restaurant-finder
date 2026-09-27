using NearbyEats.Domain.Restaurants;

namespace NearbyEats.Application.Restaurants;

public interface IRestaurantDetailsProvider
{
    Task<Restaurant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
