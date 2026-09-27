using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NearbyEats.Domain.Restaurants;
using NearbyEats.Infrastructure.Persistence;

namespace NearbyEats.Infrastructure.Tests.Persistence;

/// <summary>
/// Verifies the EF Core mapping for <see cref="Restaurant"/> at the model
/// metadata level. Uses the Npgsql provider with a dummy connection string —
/// building the model never opens a connection, so no database is required.
/// </summary>
public sealed class RestaurantModelTests
{
    private static IModel BuildModel()
    {
        var options = new DbContextOptionsBuilder<NearbyEatsDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=nearbyeats;Username=postgres;Password=changeme")
            .Options;

        using var context = new NearbyEatsDbContext(options);
        return context.Model;
    }

    [Fact]
    public void Model_Restaurant_HasIdAsPrimaryKey()
    {
        var entityType = BuildModel().FindEntityType(typeof(Restaurant));
        Assert.NotNull(entityType);

        var primaryKey = entityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal([nameof(Restaurant.Id)], primaryKey.Properties.Select(p => p.Name));
    }

    [Fact]
    public void Model_Restaurant_MapsToRestaurantsTable()
    {
        var entityType = BuildModel().FindEntityType(typeof(Restaurant));
        Assert.NotNull(entityType);
        Assert.Equal("restaurants", entityType.GetTableName());
    }

    [Fact]
    public void Model_Restaurant_Name_IsRequiredWithMaxLength()
    {
        var property = BuildModel().FindEntityType(typeof(Restaurant))!.FindProperty(nameof(Restaurant.Name));
        Assert.NotNull(property);
        Assert.False(property.IsNullable);
        Assert.Equal(200, property.GetMaxLength());
    }

    [Fact]
    public void Model_Restaurant_GooglePlaceId_HasUniqueIndex()
    {
        var model = BuildModel();
        var entityType = model.FindEntityType(typeof(Restaurant));
        Assert.NotNull(entityType);

        var property = entityType.FindProperty(nameof(Restaurant.GooglePlaceId));
        Assert.NotNull(property);
        Assert.Equal(256, property.GetMaxLength());

        var uniqueIndex = entityType.GetIndexes()
            .SingleOrDefault(i => i.Properties.Select(p => p.Name).SequenceEqual([nameof(Restaurant.GooglePlaceId)]));
        Assert.NotNull(uniqueIndex);
        Assert.True(uniqueIndex.IsUnique);
    }

    [Fact]
    public void Model_Restaurant_Location_IsOwnedWithNumericColumns()
    {
        var model = BuildModel();
        var entityType = model.FindEntityType(typeof(Restaurant));
        Assert.NotNull(entityType);

        var ownership = entityType.FindNavigation(nameof(Restaurant.Location));
        Assert.NotNull(ownership);
        Assert.True(ownership.TargetEntityType.IsOwned());

        var latitude = ownership.TargetEntityType.FindProperty(nameof(GeoLocation.Latitude));
        var longitude = ownership.TargetEntityType.FindProperty(nameof(GeoLocation.Longitude));
        Assert.NotNull(latitude);
        Assert.NotNull(longitude);
        Assert.Equal("latitude", latitude.GetColumnName());
        Assert.Equal("longitude", longitude.GetColumnName());
        Assert.Equal("numeric(9,6)", latitude.GetColumnType());
        Assert.Equal("numeric(9,6)", longitude.GetColumnType());
    }

    [Fact]
    public void Model_Restaurant_Rating_UsesSingleDecimalColumnType()
    {
        var property = BuildModel().FindEntityType(typeof(Restaurant))!.FindProperty(nameof(Restaurant.Rating));
        Assert.NotNull(property);
        Assert.Equal("numeric(2,1)", property.GetColumnType());
    }
}
