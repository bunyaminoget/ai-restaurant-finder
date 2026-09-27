using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NearbyEats.Application.Restaurants;

namespace NearbyEats.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public const string ConnectionStringName = "NearbyEats";

    public static IServiceCollection AddNearbyEatsPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"PostgreSQL connection string '{ConnectionStringName}' is not configured. " +
                "Set 'ConnectionStrings:NearbyEats' (e.g. via the ConnectionStrings__NearbyEats environment variable).");
        }

        services.AddDbContext<NearbyEatsDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IRestaurantStore, EfRestaurantStore>();

        return services;
    }
}
