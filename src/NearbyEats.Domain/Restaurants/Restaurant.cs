namespace NearbyEats.Domain.Restaurants;

public sealed class Restaurant
{
    public Guid Id { get; }
    public string Name { get; }
    public GeoLocation Location { get; }
    public double Rating { get; }
    public int ReviewCount { get; }
    public string? GooglePlaceId { get; }

    // EF Core materialization only; application code must use the validating ctor.
    private Restaurant()
    {
        Name = string.Empty;
        Location = null!;
    }

    public Restaurant(Guid id, string name, GeoLocation location, double rating, int reviewCount, string? googlePlaceId = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Id must not be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name must not be empty.", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(location);

        if (rating < 0 || rating > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(rating), rating, "Rating must be between 0 and 5.");
        }

        if (reviewCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reviewCount), reviewCount, "ReviewCount must be greater than or equal to 0.");
        }

        if (googlePlaceId is not null && string.IsNullOrWhiteSpace(googlePlaceId))
        {
            throw new ArgumentException("GooglePlaceId must not be empty when provided.", nameof(googlePlaceId));
        }

        Id = id;
        Name = name;
        Location = location;
        Rating = rating;
        ReviewCount = reviewCount;
        GooglePlaceId = googlePlaceId;
    }
}
