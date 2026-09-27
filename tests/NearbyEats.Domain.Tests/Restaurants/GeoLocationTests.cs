using NearbyEats.Domain.Restaurants;

namespace NearbyEats.Domain.Tests.Restaurants;

public sealed class GeoLocationTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(-90, -180)]
    [InlineData(90, 180)]
    [InlineData(-90, 180)]
    [InlineData(90, -180)]
    [InlineData(41.0082, 28.9784)]
    public void Ctor_WithValidCoordinates_SetsProperties(double latitude, double longitude)
    {
        var location = new GeoLocation(latitude, longitude);

        Assert.Equal(latitude, location.Latitude);
        Assert.Equal(longitude, location.Longitude);
    }

    [Theory]
    [InlineData(-90)]
    [InlineData(-89.9999)]
    [InlineData(0)]
    [InlineData(89.9999)]
    [InlineData(90)]
    public void Ctor_WithLatitudeOnOrInsideBounds_DoesNotThrow(double latitude)
    {
        var location = new GeoLocation(latitude, 0);

        Assert.Equal(latitude, location.Latitude);
    }

    [Theory]
    [InlineData(-180)]
    [InlineData(-179.9999)]
    [InlineData(0)]
    [InlineData(179.9999)]
    [InlineData(180)]
    public void Ctor_WithLongitudeOnOrInsideBounds_DoesNotThrow(double longitude)
    {
        var location = new GeoLocation(0, longitude);

        Assert.Equal(longitude, location.Longitude);
    }

    [Theory]
    [InlineData(-90.0001)]
    [InlineData(-91)]
    [InlineData(90.0001)]
    [InlineData(91)]
    [InlineData(double.MinValue)]
    [InlineData(double.MaxValue)]
    public void Ctor_WithLatitudeOutOfRange_ThrowsArgumentOutOfRangeException(double latitude)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new GeoLocation(latitude, 0));

        Assert.Equal("latitude", ex.ParamName);
    }

    [Theory]
    [InlineData(-180.0001)]
    [InlineData(-181)]
    [InlineData(180.0001)]
    [InlineData(181)]
    [InlineData(double.MinValue)]
    [InlineData(double.MaxValue)]
    public void Ctor_WithLongitudeOutOfRange_ThrowsArgumentOutOfRangeException(double longitude)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new GeoLocation(0, longitude));

        Assert.Equal("longitude", ex.ParamName);
    }

    [Fact]
    public void DistanceKmTo_WithSameCoordinates_ReturnsZero()
    {
        var location = new GeoLocation(41.0082, 28.9784);

        var distance = location.DistanceKmTo(new GeoLocation(41.0082, 28.9784));

        Assert.Equal(0, distance, 10);
    }

    [Fact]
    public void DistanceKmTo_IstanbulToAnkara_ReturnsApproximately349Km()
    {
        var istanbul = new GeoLocation(41.0082, 28.9784);
        var ankara = new GeoLocation(39.9334, 32.8597);

        var distance = istanbul.DistanceKmTo(ankara);

        Assert.InRange(distance, 348, 350);
    }

    [Fact]
    public void DistanceKmTo_OneDegreeLatitudeAtEquator_ReturnsApproximately111Km()
    {
        var origin = new GeoLocation(0, 0);
        var oneDegreeNorth = new GeoLocation(1, 0);

        var distance = origin.DistanceKmTo(oneDegreeNorth);

        Assert.Equal(111.19, distance, 2);
    }

    [Fact]
    public void DistanceKmTo_IsSymmetric()
    {
        var istanbul = new GeoLocation(41.0082, 28.9784);
        var ankara = new GeoLocation(39.9334, 32.8597);

        var forward = istanbul.DistanceKmTo(ankara);
        var reverse = ankara.DistanceKmTo(istanbul);

        Assert.Equal(forward, reverse, 10);
    }

    [Fact]
    public void DistanceKmTo_OneDegreeLongitudeAtEquator_ReturnsApproximately111Km()
    {
        var origin = new GeoLocation(0, 0);
        var oneDegreeEast = new GeoLocation(0, 1);

        var distance = origin.DistanceKmTo(oneDegreeEast);

        Assert.Equal(111.19, distance, 2);
    }

    [Fact]
    public void DistanceKmTo_OneDegreeLongitudeAtLatitude60_ReturnsApproximately55Km()
    {
        var origin = new GeoLocation(60, 0);
        var oneDegreeEast = new GeoLocation(60, 1);

        var distance = origin.DistanceKmTo(oneDegreeEast);

        Assert.Equal(55.6, distance, 1);
    }

    [Fact]
    public void DistanceKmTo_PoleToPole_ReturnsApproximately20015Km()
    {
        var northPole = new GeoLocation(90, 0);
        var southPole = new GeoLocation(-90, 0);

        var distance = northPole.DistanceKmTo(southPole);

        Assert.InRange(distance, 20010, 20020);
    }

    [Fact]
    public void DistanceKmTo_AntipodalPointsOnEquator_ReturnsApproximately20015Km()
    {
        var origin = new GeoLocation(0, 0);
        var antipode = new GeoLocation(0, 180);

        var distance = origin.DistanceKmTo(antipode);

        Assert.InRange(distance, 20010, 20020);
    }

    [Fact]
    public void DistanceKmTo_WithNull_ThrowsArgumentNullException()
    {
        var location = new GeoLocation(0, 0);

        var ex = Assert.Throws<ArgumentNullException>(() => location.DistanceKmTo(null!));

        Assert.Equal("other", ex.ParamName);
    }
}
