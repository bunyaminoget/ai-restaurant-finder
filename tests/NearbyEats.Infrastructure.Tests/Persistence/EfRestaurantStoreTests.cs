using Microsoft.EntityFrameworkCore;
using NearbyEats.Domain.Restaurants;
using NearbyEats.Infrastructure.Persistence;

namespace NearbyEats.Infrastructure.Tests.Persistence;

/// <summary>
/// Verifies the <see cref="EfRestaurantStore"/> upsert/lookup behavior using
/// the EF Core InMemory provider — no PostgreSQL or Docker required.
/// </summary>
public sealed class EfRestaurantStoreTests
{
    private static NearbyEatsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NearbyEatsDbContext>()
            .UseInMemoryDatabase($"nearbyeats-test-{Guid.NewGuid()}")
            .Options;

        return new NearbyEatsDbContext(options);
    }

    private static Restaurant CreateRestaurant(
        string placeId,
        string name = "Test Restaurant",
        double rating = 4.5,
        int reviewCount = 120) => new(
        Guid.NewGuid(),
        name,
        new GeoLocation(41.0082, 28.9784),
        rating,
        reviewCount,
        placeId);

    [Fact]
    public async Task UpsertAsync_WithNewRestaurants_InsertsAndReturnsThem()
    {
        using var context = CreateContext();
        var store = new EfRestaurantStore(context);
        var restaurants = new[] { CreateRestaurant("places/a"), CreateRestaurant("places/b") };

        var persisted = await store.UpsertAsync(restaurants);

        Assert.Equal(2, persisted.Count);
        Assert.Equal(2, await context.Restaurants.CountAsync());
        Assert.Equal("places/a", (await store.GetByGooglePlaceIdAsync("places/a"))?.GooglePlaceId);
    }

    [Fact]
    public async Task UpsertAsync_WithExistingPlaceId_UpdatesFieldsButKeepsStoredId()
    {
        using var context = CreateContext();
        var store = new EfRestaurantStore(context);
        var original = CreateRestaurant("places/a", name: "Old Name", rating: 3.0, reviewCount: 10);
        await store.UpsertAsync([original]);

        var incoming = new Restaurant(
            Guid.NewGuid(),
            "New Name",
            new GeoLocation(41.01, 28.98),
            4.8,
            300,
            "places/a");

        var persisted = await store.UpsertAsync([incoming]);

        var single = Assert.Single(persisted);
        Assert.Equal(original.Id, single.Id);
        Assert.Equal("New Name", single.Name);
        Assert.Equal(4.8, single.Rating);
        Assert.Equal(300, single.ReviewCount);
        Assert.Equal(1, await context.Restaurants.CountAsync());

        var reloaded = await store.GetByIdAsync(original.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("New Name", reloaded.Name);
        Assert.Equal("places/a", reloaded.GooglePlaceId);
    }

    [Fact]
    public async Task UpsertAsync_WithEmptyList_ReturnsEmptyWithoutSaving()
    {
        using var context = CreateContext();
        var store = new EfRestaurantStore(context);

        var persisted = await store.UpsertAsync([]);

        Assert.Empty(persisted);
        Assert.Equal(0, await context.Restaurants.CountAsync());
    }

    [Fact]
    public async Task GetByGooglePlaceIdAsync_WhenMissing_ReturnsNull()
    {
        using var context = CreateContext();
        var store = new EfRestaurantStore(context);

        Assert.Null(await store.GetByGooglePlaceIdAsync("places/unknown"));
        Assert.Null(await store.GetByGooglePlaceIdAsync("  "));
    }

    [Fact]
    public async Task UpsertAsync_PassesCancellationToken()
    {
        using var context = CreateContext();
        var store = new EfRestaurantStore(context);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => store.UpsertAsync([CreateRestaurant("places/a")], cts.Token));
    }
}
