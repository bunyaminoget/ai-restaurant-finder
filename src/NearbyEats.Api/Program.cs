using NearbyEats.Api.Restaurants;
using NearbyEats.Application.Restaurants;
using NearbyEats.Domain.Restaurants;
using NearbyEats.Infrastructure.Restaurants;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

builder.Services.AddGooglePlacesRestaurantSearch(builder.Configuration);
builder.Services.AddTransient<SearchNearbyRestaurantsHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapHealthChecks("/health");

app.MapGet("/api/restaurants/nearby", async (
    double latitude,
    double longitude,
    double? radiusKm,
    double? minRating,
    string? sortBy,
    string? sortDirection,
    int? page,
    int? pageSize,
    SearchNearbyRestaurantsHandler handler,
    CancellationToken cancellationToken) =>
{
    if (latitude is < -90 or > 90)
    {
        return Results.BadRequest("Latitude must be between -90 and 90.");
    }

    if (longitude is < -180 or > 180)
    {
        return Results.BadRequest("Longitude must be between -180 and 180.");
    }

    var radiusKmValue = radiusKm ?? 2;
    if (radiusKmValue is < 0.1 or > 5)
    {
        return Results.BadRequest("RadiusKm must be between 0.1 and 5.");
    }

    if (minRating.HasValue && minRating.Value is < 0 or > 5)
    {
        return Results.BadRequest("MinRating must be between 0 and 5.");
    }

    var sortByRaw = sortBy ?? nameof(RestaurantSortBy.Rating);
    if (!Enum.GetNames<RestaurantSortBy>().Contains(sortByRaw, StringComparer.OrdinalIgnoreCase) ||
        !Enum.TryParse<RestaurantSortBy>(sortByRaw, ignoreCase: true, out var sortByValue))
    {
        return Results.BadRequest("Invalid sortBy. Allowed values: rating, distance, reviewCount.");
    }

    var sortDirectionRaw = sortDirection ?? nameof(SortDirection.Desc);
    if (!Enum.GetNames<SortDirection>().Contains(sortDirectionRaw, StringComparer.OrdinalIgnoreCase) ||
        !Enum.TryParse<SortDirection>(sortDirectionRaw, ignoreCase: true, out var sortDirectionValue))
    {
        return Results.BadRequest("Invalid sortDirection. Allowed values: asc, desc.");
    }

    var pageValue = page ?? 1;
    if (pageValue < 1)
    {
        return Results.BadRequest("Page must be greater than or equal to 1.");
    }

    var pageSizeValue = pageSize ?? 20;
    if (pageSizeValue is < 1 or > 20)
    {
        return Results.BadRequest("PageSize must be between 1 and 20.");
    }

    var center = new GeoLocation(latitude, longitude);
    var query = new SearchNearbyRestaurantsQuery(
        center.Latitude,
        center.Longitude,
        radiusKmValue,
        minRating,
        sortByValue,
        sortDirectionValue,
        pageValue,
        pageSizeValue);
    var result = await handler.HandleAsync(query, cancellationToken).ConfigureAwait(false);

    var response = new NearbyRestaurantsResponse(
        result.Items,
        new NearbyRestaurantsPaginationResponse(
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages,
            result.HasNextPage));

    return Results.Ok(response);
});

app.Run();
