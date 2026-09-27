using NearbyEats.Application.Restaurants;
using NearbyEats.Domain.Restaurants;

namespace NearbyEats.Application.Tests.Restaurants;

public sealed class GetRestaurantDetailHandlerTests
{
    private sealed class FakeStore : IRestaurantStore
    {
        private readonly Func<Guid, Restaurant?> _resolve;

        public Guid? LastRequestedId { get; private set; }

        public FakeStore(Func<Guid, Restaurant?> resolve)
        {
            _resolve = resolve;
        }

        public Task<Restaurant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastRequestedId = id;
            return Task.FromResult(_resolve(id));
        }

        public Task<Restaurant?> GetByGooglePlaceIdAsync(string googlePlaceId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<Restaurant>> UpsertAsync(IReadOnlyList<Restaurant> restaurants, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new NotSupportedException();
        }
    }

    private sealed class FakeDetailsProvider : IRestaurantDetailsProvider
    {
        private readonly Func<string, Restaurant?> _resolve;

        public string? LastRequestedPlaceId { get; private set; }

        public FakeDetailsProvider(Func<string, Restaurant?> resolve)
        {
            _resolve = resolve;
        }

        public Task<Restaurant?> GetByPlaceIdAsync(string googlePlaceId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastRequestedPlaceId = googlePlaceId;
            return Task.FromResult(_resolve(googlePlaceId));
        }
    }

    private static Restaurant CreateRestaurant(Guid? id = null, string? googlePlaceId = "places/test-place-id") => new(
        id ?? Guid.NewGuid(),
        "Test Restaurant",
        new GeoLocation(41.0082, 28.9784),
        4.5,
        120,
        googlePlaceId);

    private static GetRestaurantDetailHandler CreateHandler(
        Func<Guid, Restaurant?> resolveStored,
        Func<string, Restaurant?> resolveDetails,
        out FakeStore store,
        out FakeDetailsProvider provider)
    {
        store = new FakeStore(resolveStored);
        provider = new FakeDetailsProvider(resolveDetails);
        return new GetRestaurantDetailHandler(store, provider);
    }

    [Fact]
    public void Ctor_WithNullStore_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => new GetRestaurantDetailHandler(null!, new FakeDetailsProvider(_ => null)));

        Assert.Equal("store", ex.ParamName);
    }

    [Fact]
    public void Ctor_WithNullProvider_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => new GetRestaurantDetailHandler(new FakeStore(_ => null), null!));

        Assert.Equal("provider", ex.ParamName);
    }

    [Fact]
    public async Task HandleAsync_WithNullQuery_ThrowsArgumentNullException()
    {
        var handler = CreateHandler(_ => CreateRestaurant(), _ => CreateRestaurant(), out _, out _);

        await Assert.ThrowsAsync<ArgumentNullException>(() => handler.HandleAsync(null!));
    }

    [Fact]
    public async Task HandleAsync_WithEmptyId_ThrowsArgumentException()
    {
        var handler = CreateHandler(_ => CreateRestaurant(), _ => CreateRestaurant(), out var store, out var provider);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => handler.HandleAsync(new GetRestaurantDetailQuery(Guid.Empty)));

        Assert.Equal("query", ex.ParamName);
        Assert.Null(store.LastRequestedId);
        Assert.Null(provider.LastRequestedPlaceId);
    }

    [Fact]
    public async Task HandleAsync_WhenStoreMisses_ReturnsNullWithoutCallingProvider()
    {
        var id = Guid.NewGuid();
        var handler = CreateHandler(_ => null, _ => CreateRestaurant(), out var store, out var provider);

        var result = await handler.HandleAsync(new GetRestaurantDetailQuery(id));

        Assert.Null(result);
        Assert.Equal(id, store.LastRequestedId);
        Assert.Null(provider.LastRequestedPlaceId);
    }

    [Fact]
    public async Task HandleAsync_WhenStoredRowHasNoPlaceId_ReturnsNullWithoutCallingProvider()
    {
        var stored = CreateRestaurant(googlePlaceId: null);
        var handler = CreateHandler(_ => stored, _ => CreateRestaurant(), out _, out var provider);

        var result = await handler.HandleAsync(new GetRestaurantDetailQuery(stored.Id));

        Assert.Null(result);
        Assert.Null(provider.LastRequestedPlaceId);
    }

    [Fact]
    public async Task HandleAsync_WhenProviderReturnsNull_ReturnsNull()
    {
        var stored = CreateRestaurant();
        var handler = CreateHandler(_ => stored, _ => null, out _, out var provider);

        var result = await handler.HandleAsync(new GetRestaurantDetailQuery(stored.Id));

        Assert.Null(result);
        Assert.Equal(stored.GooglePlaceId, provider.LastRequestedPlaceId);
    }

    [Fact]
    public async Task HandleAsync_WhenProviderReturnsMismatchedId_ReturnsNull()
    {
        var stored = CreateRestaurant();
        var other = CreateRestaurant(googlePlaceId: stored.GooglePlaceId);
        var handler = CreateHandler(_ => stored, _ => other, out _, out _);

        var result = await handler.HandleAsync(new GetRestaurantDetailQuery(stored.Id));

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_WhenDetailsResolve_MapsAllFields()
    {
        var stored = CreateRestaurant();
        var fresh = new Restaurant(
            stored.Id,
            "Updated Name",
            new GeoLocation(41.01, 28.98),
            4.7,
            250,
            stored.GooglePlaceId);
        var handler = CreateHandler(_ => stored, _ => fresh, out _, out var provider);

        var result = await handler.HandleAsync(new GetRestaurantDetailQuery(stored.Id));

        Assert.NotNull(result);
        Assert.Equal(fresh.Id, result.Id);
        Assert.Equal(fresh.Name, result.Name);
        Assert.Equal(fresh.Location.Latitude, result.Latitude);
        Assert.Equal(fresh.Location.Longitude, result.Longitude);
        Assert.Equal(fresh.Rating, result.Rating);
        Assert.Equal(fresh.ReviewCount, result.ReviewCount);
        Assert.Equal(stored.GooglePlaceId, provider.LastRequestedPlaceId);
    }

    [Fact]
    public async Task HandleAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        var stored = CreateRestaurant();
        var handler = CreateHandler(_ => stored, _ => stored, out _, out _);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.HandleAsync(new GetRestaurantDetailQuery(stored.Id), cts.Token));
    }
}
