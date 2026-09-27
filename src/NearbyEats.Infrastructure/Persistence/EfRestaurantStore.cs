using Microsoft.EntityFrameworkCore;
using NearbyEats.Application.Restaurants;
using NearbyEats.Domain.Restaurants;

namespace NearbyEats.Infrastructure.Persistence;

public sealed class EfRestaurantStore : IRestaurantStore
{
    private readonly NearbyEatsDbContext _dbContext;

    public EfRestaurantStore(NearbyEatsDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<Restaurant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        return await _dbContext.Restaurants
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Restaurant?> GetByGooglePlaceIdAsync(string googlePlaceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(googlePlaceId))
        {
            return null;
        }

        return await _dbContext.Restaurants
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.GooglePlaceId == googlePlaceId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Restaurant>> UpsertAsync(IReadOnlyList<Restaurant> restaurants, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(restaurants);

        if (restaurants.Count == 0)
        {
            return restaurants;
        }

        // Single round trip: load every row that could match either by Google
        // place id (provider identity) or by public id (rows persisted without
        // a place id, or a place id added later).
        var placeIds = restaurants
            .Select(r => r.GooglePlaceId)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct()
            .ToList();
        var ids = restaurants.Select(r => r.Id).ToList();

        var existing = await _dbContext.Restaurants
            .Where(r => (r.GooglePlaceId != null && placeIds.Contains(r.GooglePlaceId)) || ids.Contains(r.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var byPlaceId = new Dictionary<string, Restaurant>(StringComparer.Ordinal);
        var byId = new Dictionary<Guid, Restaurant>();
        foreach (var row in existing)
        {
            if (row.GooglePlaceId is not null)
            {
                byPlaceId[row.GooglePlaceId] = row;
            }

            byId[row.Id] = row;
        }

        var persisted = new List<Restaurant>(restaurants.Count);
        foreach (var incoming in restaurants)
        {
            Restaurant? match = null;
            if (!string.IsNullOrWhiteSpace(incoming.GooglePlaceId) &&
                byPlaceId.TryGetValue(incoming.GooglePlaceId, out var byPlace))
            {
                match = byPlace;
            }
            else if (byId.TryGetValue(incoming.Id, out var byGuid))
            {
                match = byGuid;
            }

            if (match is null)
            {
                _dbContext.Restaurants.Add(incoming);
                persisted.Add(incoming);
            }
            else
            {
                // Refresh Google-provided fields but keep the stored id so
                // public ids never change. Restaurant exposes no setters, so
                // swap the tracked instance for a rebuilt one with the same key.
                _dbContext.Entry(match).State = EntityState.Detached;
                var refreshed = new Restaurant(
                    match.Id,
                    incoming.Name,
                    incoming.Location,
                    incoming.Rating,
                    incoming.ReviewCount,
                    match.GooglePlaceId ?? incoming.GooglePlaceId);
                _dbContext.Restaurants.Update(refreshed);
                persisted.Add(refreshed);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return persisted;
    }
}
