using NearbyEats.Application.Restaurants;

namespace NearbyEats.Api.Restaurants;

public sealed record RestaurantDetailResponse(
    Guid Id,
    string Name,
    double Latitude,
    double Longitude,
    double Rating,
    int ReviewCount)
{
    public static RestaurantDetailResponse FromDto(RestaurantDetailDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new RestaurantDetailResponse(
            dto.Id,
            dto.Name,
            dto.Latitude,
            dto.Longitude,
            dto.Rating,
            dto.ReviewCount);
    }
}
