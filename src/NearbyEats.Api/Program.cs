using Microsoft.AspNetCore.Diagnostics;
using NearbyEats.Api.Restaurants;
using NearbyEats.Application.Restaurants;
using NearbyEats.Domain.Restaurants;
using NearbyEats.Infrastructure.Persistence;
using NearbyEats.Infrastructure.Restaurants;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

builder.Services.AddGooglePlacesRestaurantSearch(builder.Configuration);
builder.Services.AddNearbyEatsPersistence(builder.Configuration);
builder.Services.AddTransient<SearchNearbyRestaurantsHandler>();
builder.Services.AddTransient<GetRestaurantDetailHandler>();

var app = builder.Build();

// Unhandled provider/infrastructure failures (missing API key, upstream Google
// errors) must surface as JSON ProblemDetails, not the default HTML error page.
// Explicit 400/404 plain-text responses below are unaffected: they do not throw.
app.UseExceptionHandler(exceptionHandlerApp => exceptionHandlerApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var logger = context.RequestServices
        .GetService<ILoggerFactory>()
        ?.CreateLogger("GlobalExceptionHandler");

    // Canceled requests (OperationCanceledException covers TaskCanceledException,
    // e.g. HttpClient timeouts) must not surface as 500. When the client went
    // away, never attempt to write a ProblemDetails body to a dead connection.
    if (exception is OperationCanceledException)
    {
        if (context.RequestAborted.IsCancellationRequested || context.Response.HasStarted)
        {
            logger?.LogWarning(exception, "Request was canceled by the client; skipping error response.");
            return;
        }

        logger?.LogWarning(exception, "Upstream restaurant data provider request timed out or was canceled.");
        await Results.Problem(title: "Restaurant data provider timed out.", statusCode: StatusCodes.Status504GatewayTimeout).ExecuteAsync(context).ConfigureAwait(false);
        return;
    }

    var (statusCode, title) = exception switch
    {
        InvalidOperationException => (StatusCodes.Status503ServiceUnavailable, "Restaurant data provider is not configured."),
        HttpRequestException => (StatusCodes.Status502BadGateway, "Restaurant data provider is temporarily unavailable."),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
    };

    // The exception message may carry the upstream error body; keep it in
    // server logs only, never in the ProblemDetails response.
    if (exception is HttpRequestException)
    {
        logger?.LogWarning(exception, "Restaurant data provider request failed with {StatusCode}.", statusCode);
    }
    else
    {
        logger?.LogError(exception, "Unhandled error serving request with {StatusCode}.", statusCode);
    }

    await Results.Problem(title: title, statusCode: statusCode).ExecuteAsync(context).ConfigureAwait(false);
}));

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

app.MapGet("/api/restaurants/{id}", async (
    string id,
    GetRestaurantDetailHandler handler,
    CancellationToken cancellationToken) =>
{
    if (!Guid.TryParse(id, out var restaurantId) || restaurantId == Guid.Empty)
    {
        return Results.BadRequest("Id must be a valid non-empty GUID.");
    }

    var dto = await handler.HandleAsync(new GetRestaurantDetailQuery(restaurantId), cancellationToken).ConfigureAwait(false);

    if (dto is null)
    {
        return Results.NotFound($"Restaurant with id '{restaurantId}' was not found.");
    }

    return Results.Ok(RestaurantDetailResponse.FromDto(dto));
});

app.Run();
