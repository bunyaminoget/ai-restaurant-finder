using Microsoft.EntityFrameworkCore;
using NearbyEats.Domain.Restaurants;

namespace NearbyEats.Infrastructure.Persistence;

public sealed class NearbyEatsDbContext : DbContext
{
    public NearbyEatsDbContext(DbContextOptions<NearbyEatsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Restaurant> Restaurants => Set<Restaurant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NearbyEatsDbContext).Assembly);
    }
}
