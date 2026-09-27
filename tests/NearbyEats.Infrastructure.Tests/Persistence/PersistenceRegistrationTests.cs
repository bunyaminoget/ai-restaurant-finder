using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NearbyEats.Application.Restaurants;
using NearbyEats.Infrastructure.Persistence;

namespace NearbyEats.Infrastructure.Tests.Persistence;

/// <summary>
/// Verifies the persistence DI wiring without touching a real database:
/// resolving the context/store never opens a connection.
/// </summary>
public sealed class PersistenceRegistrationTests
{
    private static IConfiguration ConfigurationWithConnectionString(string? connectionString)
    {
        var values = connectionString is null
            ? new Dictionary<string, string?>()
            : new Dictionary<string, string?>
            {
                ["ConnectionStrings:NearbyEats"] = connectionString
            };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    [Fact]
    public void AddNearbyEatsPersistence_RegistersDbContextAndStore()
    {
        var services = new ServiceCollection();
        services.AddNearbyEatsPersistence(
            ConfigurationWithConnectionString("Host=localhost;Port=5432;Database=nearbyeats;Username=postgres;Password=changeme"));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<NearbyEatsDbContext>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IRestaurantStore>());
    }

    [Fact]
    public void AddNearbyEatsPersistence_WithoutConnectionString_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(
            () => services.AddNearbyEatsPersistence(ConfigurationWithConnectionString(null)));
    }
}
