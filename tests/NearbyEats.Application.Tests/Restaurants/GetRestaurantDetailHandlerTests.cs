using NearbyEats.Application.Restaurants;
using NearbyEats.Domain.Restaurants;

namespace NearbyEats.Application.Tests.Restaurants;

public sealed class GetRestaurantDetailHandlerTests
{
    private sealed class FakeDetailsProvider : IRestaurantDetailsProvider
    {
        private readonly Func<Guid, Restaurant?> _resolve;

        public Guid? LastRequestedId { get; private set; }

        public FakeDetailsProvider(Func<Guid, Restaurant?> resolve)
        {
            _resolve = resolve;
        }

        public Task<Restaurant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastRequestedId = id;
            return Task.FromResult(_resolve(id));
        }
    }

    private static Restaurant CreateRestaurant(Guid? id = null) => new(
        id ?? Guid.NewGuid(),
        "Test Restaurant",
        new GeoLocation(41.0082, 28.9784),
        4.5,
        120);

    [Fact]
    public void Ctor_WithNullProvider_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new GetRestaurantDetailHandler(null!));

        Assert.Equal("provider", ex.ParamName);
    }

    [Fact]
    public async Task HandleAsync_WithNullQuery_ThrowsArgumentNullException()
    {
        var handler = new GetRestaurantDetailHandler(new FakeDetailsProvider(_ => CreateRestaurant()));

        await Assert.ThrowsAsync<ArgumentNullException>(() => handler.HandleAsync(null!));
    }

    [Fact]
    public async Task HandleAsync_WithEmptyId_ThrowsArgumentException()
    {
        var provider = new FakeDetailsProvider(_ => CreateRestaurant());
        var handler = new GetRestaurantDetailHandler(provider);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => handler.HandleAsync(new GetRestaurantDetailQuery(Guid.Empty)));

        Assert.Equal("query", ex.ParamName);
        Assert.Null(provider.LastRequestedId);
    }

    [Fact]
    public async Task HandleAsync_WhenProviderReturnsNull_ReturnsNull()
    {
        var id = Guid.NewGuid();
        var provider = new FakeDetailsProvider(_ => null);
        var handler = new GetRestaurantDetailHandler(provider);

        var result = await handler.HandleAsync(new GetRestaurantDetailQuery(id));

        Assert.Null(result);
        Assert.Equal(id, provider.LastRequestedId);
    }

    [Fact]
    public async Task HandleAsync_WhenProviderReturnsRestaurant_MapsAllFields()
    {
        var restaurant = CreateRestaurant();
        var handler = new GetRestaurantDetailHandler(new FakeDetailsProvider(_ => restaurant));

        var result = await handler.HandleAsync(new GetRestaurantDetailQuery(restaurant.Id));

        Assert.NotNull(result);
        Assert.Equal(restaurant.Id, result.Id);
        Assert.Equal(restaurant.Name, result.Name);
        Assert.Equal(restaurant.Location.Latitude, result.Latitude);
        Assert.Equal(restaurant.Location.Longitude, result.Longitude);
        Assert.Equal(restaurant.Rating, result.Rating);
        Assert.Equal(restaurant.ReviewCount, result.ReviewCount);
    }

    [Fact]
    public async Task HandleAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        var restaurant = CreateRestaurant();
        var handler = new GetRestaurantDetailHandler(new FakeDetailsProvider(_ => restaurant));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.HandleAsync(new GetRestaurantDetailQuery(restaurant.Id), cts.Token));
    }
}
