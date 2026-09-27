using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using NearbyEats.Application.Restaurants;
using NearbyEats.Domain.Restaurants;

namespace NearbyEats.Infrastructure.Restaurants;

public sealed class GooglePlacesRestaurantSearchProvider : IRestaurantSearchProvider, IRestaurantDetailsProvider
{
    private const string HttpClientName = "GooglePlaces";
    private const string SearchNearbyPath = "v1/places:searchNearby";
    private const string PlaceDetailsPathPrefix = "v1/places/";
    // Deliberate limitation (M1): the API radiusKm filter is applied in memory
    // by the handler; the provider always fetches this fixed 2 km window and
    // IRestaurantSearchProvider only takes the center point.
    private const double SearchRadiusMeters = 2000;
    // Deliberate limitation (M3): single page only, no nextPageToken loop; the
    // handler's Skip/Take pagination reflects this fetched window, not the full
    // upstream result set.
    private const int MaxResultCount = 20;
    private const string SearchFieldMask = "places.id,places.displayName,places.location,places.rating,places.userRatingCount";
    private const string DetailsFieldMask = "id,displayName,location,rating,userRatingCount";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GooglePlacesOptions _options;

    public GooglePlacesRestaurantSearchProvider(IHttpClientFactory httpClientFactory, IOptions<GooglePlacesOptions> options)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(options);

        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<Restaurant>> SearchNearbyAsync(GeoLocation center, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(center);

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Google Places API key is not configured. Set 'GooglePlaces:ApiKey' in configuration.");
        }

        var client = _httpClientFactory.CreateClient(HttpClientName);

        var requestBody = new
        {
            includedTypes = new[] { "restaurant" },
            maxResultCount = MaxResultCount,
            locationRestriction = new
            {
                circle = new
                {
                    center = new { latitude = center.Latitude, longitude = center.Longitude },
                    radius = SearchRadiusMeters
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, SearchNearbyPath)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody, JsonOptions), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Goog-Api-Key", _options.ApiKey);
        request.Headers.Add("X-Goog-FieldMask", SearchFieldMask);

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException(
                $"Google Places Nearby Search failed with status {(int)response.StatusCode} ({response.StatusCode}). Response body: {errorBody}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var searchResponse = JsonSerializer.Deserialize<GooglePlacesSearchNearbyResponse>(responseJson, JsonOptions);

        if (searchResponse?.Places is null || searchResponse.Places.Count == 0)
        {
            return Array.Empty<Restaurant>();
        }

        var restaurants = new List<Restaurant>(searchResponse.Places.Count);
        foreach (var place in searchResponse.Places)
        {
            var restaurant = MapToRestaurant(place);
            if (restaurant is not null)
            {
                restaurants.Add(restaurant);
            }
        }

        return restaurants;
    }

    /// <summary>
    /// Fetches fresh details for a Google place id. Callers (see
    /// <c>GetRestaurantDetailHandler</c>) resolve the public restaurant id to
    /// its place id through the <c>IRestaurantStore</c> database lookup first,
    /// so this provider keeps no process-local id registry and works across
    /// restarts and load-balanced instances.
    /// </summary>
    public async Task<Restaurant?> GetByPlaceIdAsync(string googlePlaceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(googlePlaceId))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Google Places API key is not configured. Set 'GooglePlaces:ApiKey' in configuration.");
        }

        var client = _httpClientFactory.CreateClient(HttpClientName);

        var detailsPath = PlaceDetailsPathPrefix + Uri.EscapeDataString(NormalizePlaceIdForDetails(googlePlaceId));
        using var request = new HttpRequestMessage(HttpMethod.Get, detailsPath);
        request.Headers.Add("X-Goog-Api-Key", _options.ApiKey);
        request.Headers.Add("X-Goog-FieldMask", DetailsFieldMask);

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException(
                $"Google Places Place Details failed with status {(int)response.StatusCode} ({response.StatusCode}). Response body: {errorBody}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var place = JsonSerializer.Deserialize<GooglePlaceDto>(responseJson, JsonOptions);

        if (place is null)
        {
            return null;
        }

        var restaurant = MapToRestaurant(place);

        return restaurant;
    }

    private Restaurant? MapToRestaurant(GooglePlaceDto place)
    {
        if (string.IsNullOrWhiteSpace(place.Id))
        {
            return null;
        }

        var name = place.DisplayName?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        if (place.Location is null)
        {
            return null;
        }

        GeoLocation location;
        try
        {
            location = new GeoLocation(place.Location.Latitude, place.Location.Longitude);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }

        var rating = place.Rating ?? 0;
        if (rating < 0 || rating > 5)
        {
            rating = 0;
        }

        var reviewCount = place.UserRatingCount ?? 0;
        if (reviewCount < 0)
        {
            reviewCount = 0;
        }

        var restaurantId = CreateDeterministicGuid(place.Id);

        return new Restaurant(restaurantId, name, location, rating, reviewCount, place.Id);
    }

    private static string NormalizePlaceIdForDetails(string placeId)
    {
        // Place Details expects the bare resource id in v1/places/{id}; strip a
        // "places/" prefix when present so it is not encoded into
        // v1/places/places%2Fxxx.
        const string resourcePrefix = "places/";
        return placeId.StartsWith(resourcePrefix, StringComparison.OrdinalIgnoreCase)
            ? placeId[resourcePrefix.Length..]
            : placeId;
    }

    private static Guid CreateDeterministicGuid(string placeId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(placeId));

        Span<byte> guidBytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(guidBytes);

        var guid = new Guid(guidBytes);
        return guid == Guid.Empty ? new Guid("00000000-0000-0000-0000-000000000001") : guid;
    }

    private sealed class GooglePlacesSearchNearbyResponse
    {
        [JsonPropertyName("places")]
        public List<GooglePlaceDto>? Places { get; set; }
    }

    private sealed class GooglePlaceDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("displayName")]
        public GoogleDisplayNameDto? DisplayName { get; set; }

        [JsonPropertyName("location")]
        public GoogleLocationDto? Location { get; set; }

        [JsonPropertyName("rating")]
        public double? Rating { get; set; }

        [JsonPropertyName("userRatingCount")]
        public int? UserRatingCount { get; set; }
    }

    private sealed class GoogleDisplayNameDto
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private sealed class GoogleLocationDto
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }
}
