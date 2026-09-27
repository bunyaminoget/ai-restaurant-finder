using NearbyEats.Application.Restaurants;
using NearbyEats.Domain.Restaurants;

namespace NearbyEats.Application.Tests.Restaurants;

public sealed class SearchNearbyRestaurantsHandlerTests
{
    private sealed class FakeSearchProvider : IRestaurantSearchProvider
    {
        private readonly IReadOnlyList<Restaurant> _results;

        public FakeSearchProvider(IReadOnlyList<Restaurant> results)
        {
            _results = results;
        }

        public Task<IReadOnlyList<Restaurant>> SearchNearbyAsync(GeoLocation center, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_results);
        }
    }

    private sealed class FakeStore : IRestaurantStore
    {
        private readonly Func<IReadOnlyList<Restaurant>, IReadOnlyList<Restaurant>> _upsert;

        public IReadOnlyList<Restaurant>? LastUpserted { get; private set; }
        public int UpsertCalls { get; private set; }

        public FakeStore(Func<IReadOnlyList<Restaurant>, IReadOnlyList<Restaurant>>? upsert = null)
        {
            _upsert = upsert ?? (r => r);
        }

        public Task<Restaurant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new NotSupportedException();
        }

        public Task<Restaurant?> GetByGooglePlaceIdAsync(string googlePlaceId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<Restaurant>> UpsertAsync(IReadOnlyList<Restaurant> restaurants, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            UpsertCalls++;
            LastUpserted = restaurants;
            return Task.FromResult(_upsert(restaurants));
        }
    }

    private static Restaurant CreateRestaurant(string placeId, double rating = 4.5, int reviewCount = 100) => new(
        Guid.NewGuid(),
        $"Restaurant {placeId}",
        new GeoLocation(41.0082, 28.9784),
        rating,
        reviewCount,
        $"places/{placeId}");

    private static SearchNearbyRestaurantsQuery CreateQuery() => new(41.0082, 28.9784);

    [Fact]
    public void Ctor_WithNullProvider_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => new SearchNearbyRestaurantsHandler(null!, new FakeStore()));

        Assert.Equal("provider", ex.ParamName);
    }

    [Fact]
    public void Ctor_WithNullStore_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => new SearchNearbyRestaurantsHandler(new FakeSearchProvider([]), null!));

        Assert.Equal("store", ex.ParamName);
    }

    [Fact]
    public async Task HandleAsync_PersistsProviderResults_BeforeFiltering()
    {
        var providerResults = new[] { CreateRestaurant("a"), CreateRestaurant("b") };
        var store = new FakeStore();
        var handler = new SearchNearbyRestaurantsHandler(new FakeSearchProvider(providerResults), store);

        var result = await handler.HandleAsync(CreateQuery());

        Assert.Equal(1, store.UpsertCalls);
        Assert.Same(providerResults, store.LastUpserted);
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_UsesPersistedInstances_ForResponse()
    {
        var providerResults = new[] { CreateRestaurant("a") };
        var persistedReplacement = new Restaurant(
            Guid.NewGuid(),
            "Persisted Name",
            new GeoLocation(41.0082, 28.9784),
            4.0,
            50,
            "places/a");
        var store = new FakeStore(_ => new[] { persistedReplacement });
        var handler = new SearchNearbyRestaurantsHandler(new FakeSearchProvider(providerResults), store);

        var result = await handler.HandleAsync(CreateQuery());

        var item = Assert.Single(result.Items);
        Assert.Equal(persistedReplacement.Id, item.Id);
        Assert.Equal("Persisted Name", item.Name);
    }

    [Fact]
    public async Task HandleAsync_WhenProviderReturnsEmpty_SkipsUpsert()
    {
        var store = new FakeStore();
        var handler = new SearchNearbyRestaurantsHandler(new FakeSearchProvider([]), store);

        var result = await handler.HandleAsync(CreateQuery());

        Assert.Equal(0, store.UpsertCalls);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task HandleAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        var store = new FakeStore();
        var handler = new SearchNearbyRestaurantsHandler(
            new FakeSearchProvider([CreateRestaurant("a")]),
            store);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.HandleAsync(CreateQuery(), cts.Token));
    }
}
