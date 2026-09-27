using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NearbyEats.Domain.Restaurants;

namespace NearbyEats.Infrastructure.Persistence;

public sealed class RestaurantEntityConfiguration : IEntityTypeConfiguration<Restaurant>
{
    public void Configure(EntityTypeBuilder<Restaurant> builder)
    {
        builder.ToTable("restaurants");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(200);

        // Optional in the model (the validating ctor allows null for
        // pre-persistence instances), but unique when present so a Google
        // place maps to at most one row. PostgreSQL treats NULLs as distinct,
        // so multiple rows without a place id do not conflict.
        builder.Property(r => r.GooglePlaceId)
            .HasMaxLength(256);

        builder.HasIndex(r => r.GooglePlaceId)
            .IsUnique();

        // Domain keeps double; storage uses exact numerics. Six fractional
        // digits (~11 cm) is more than enough for restaurant coordinates.
        var coordinateConverter = new ValueConverter<double, decimal>(
            value => Convert.ToDecimal(value),
            value => Convert.ToDouble(value));

        builder.OwnsOne(r => r.Location, location =>
        {
            location.Property(l => l.Latitude)
                .HasColumnName("latitude")
                .HasColumnType("numeric(9,6)")
                .HasConversion(coordinateConverter)
                .IsRequired();

            location.Property(l => l.Longitude)
                .HasColumnName("longitude")
                .HasColumnType("numeric(9,6)")
                .HasConversion(coordinateConverter)
                .IsRequired();
        });

        // Google Places ratings carry a single fractional digit on a 0-5
        // scale; numeric(2,1) stores them exactly without implying the false
        // precision numeric(3,2) would suggest.
        builder.Property(r => r.Rating)
            .HasColumnType("numeric(2,1)")
            .HasConversion(
                value => Convert.ToDecimal(value),
                value => Convert.ToDouble(value));

        builder.Property(r => r.ReviewCount);
    }
}
