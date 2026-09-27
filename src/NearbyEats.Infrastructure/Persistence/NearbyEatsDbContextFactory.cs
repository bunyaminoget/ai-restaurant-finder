using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NearbyEats.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can create the context without a
/// running API host. The connection string is only used to build the model —
/// no connection is opened — so the local fallback below never touches a
/// real database. Real environments override via NEARBYEATS_CONNECTION_STRING.
/// </summary>
public sealed class NearbyEatsDbContextFactory : IDesignTimeDbContextFactory<NearbyEatsDbContext>
{
    public NearbyEatsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("NEARBYEATS_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=nearbyeats;Username=postgres;Password=changeme";

        var options = new DbContextOptionsBuilder<NearbyEatsDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new NearbyEatsDbContext(options);
    }
}
