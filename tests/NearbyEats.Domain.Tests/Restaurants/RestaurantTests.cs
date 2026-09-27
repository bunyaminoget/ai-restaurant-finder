using NearbyEats.Domain.Restaurants;

namespace NearbyEats.Domain.Tests.Restaurants;

public sealed class RestaurantTests
{
    private static GeoLocation ValidLocation() => new(41.0082, 28.9784);

    private static Restaurant CreateValid(
        Guid? id = null,
        string? name = "Test Restaurant",
        GeoLocation? location = null,
        double rating = 4.5,
        int reviewCount = 10)
    {
        return new Restaurant(
            id ?? Guid.NewGuid(),
            name!,
            location ?? ValidLocation(),
            rating,
            reviewCount);
    }

    [Fact]
    public void Ctor_WithValidArguments_SetsProperties()
    {
        var id = Guid.NewGuid();
        var location = ValidLocation();

        var restaurant = new Restaurant(id, "Test Restaurant", location, 4.5, 10);

        Assert.Equal(id, restaurant.Id);
        Assert.Equal("Test Restaurant", restaurant.Name);
        Assert.Same(location, restaurant.Location);
        Assert.Equal(4.5, restaurant.Rating);
        Assert.Equal(10, restaurant.ReviewCount);
    }

    [Fact]
    public void Ctor_WithEmptyId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => CreateValid(id: Guid.Empty));

        Assert.Equal("id", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Ctor_WithNullEmptyOrWhitespaceName_ThrowsArgumentException(string? name)
    {
        var ex = Assert.Throws<ArgumentException>(
            () => CreateValid(name: name));

        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    public void Ctor_WithNullLocation_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => new Restaurant(Guid.NewGuid(), "Test Restaurant", null!, 4.5, 10));

        Assert.Equal("location", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(2.5)]
    public void Ctor_WithRatingOnOrInsideBounds_DoesNotThrow(double rating)
    {
        var restaurant = CreateValid(rating: rating);

        Assert.Equal(rating, restaurant.Rating);
    }

    [Theory]
    [InlineData(-0.0001)]
    [InlineData(-1)]
    [InlineData(5.0001)]
    [InlineData(6)]
    [InlineData(double.MinValue)]
    [InlineData(double.MaxValue)]
    public void Ctor_WithRatingOutOfRange_ThrowsArgumentOutOfRangeException(double rating)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateValid(rating: rating));

        Assert.Equal("rating", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void Ctor_WithNonNegativeReviewCount_DoesNotThrow(int reviewCount)
    {
        var restaurant = CreateValid(reviewCount: reviewCount);

        Assert.Equal(reviewCount, restaurant.ReviewCount);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void Ctor_WithNegativeReviewCount_ThrowsArgumentOutOfRangeException(int reviewCount)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateValid(reviewCount: reviewCount));

        Assert.Equal("reviewCount", ex.ParamName);
    }
}
