using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NearbyEats.Application.Restaurants;

namespace NearbyEats.Infrastructure.Restaurants;

public static class GooglePlacesServiceCollectionExtensions
{
    public static IServiceCollection AddGooglePlacesRestaurantSearch(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<GooglePlacesOptions>()
            .Bind(configuration.GetSection(GooglePlacesOptions.SectionName));

        AddHttpClient(services);

        return services;
    }

    public static IServiceCollection AddGooglePlacesRestaurantSearch(
        this IServiceCollection services,
        Action<GooglePlacesOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<GooglePlacesOptions>()
            .Configure(configure);

        AddHttpClient(services);

        return services;
    }

    private static void AddHttpClient(IServiceCollection services)
    {
        services.AddHttpClient("GooglePlaces", client =>
        {
            client.BaseAddress = new Uri("https://places.googleapis.com/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddTransient<IRestaurantSearchProvider, GooglePlacesRestaurantSearchProvider>();
    }
}
