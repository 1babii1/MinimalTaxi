using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MinimalTaxiService.Domain.Entities;

namespace MinimalTaxiService.Infrastructure.Postgres.Configurations;

public class IntercityTripDetailsConfiguration : IEntityTypeConfiguration<IntercityTripDetails>
{
    public void Configure(EntityTypeBuilder<IntercityTripDetails> builder)
    {
        builder.ToTable("intercity_trip_details");

        builder.HasKey(details => details.Id).HasName("pk_intercity_trip_details");

        builder.Property(details => details.Id)
            .HasColumnName("id");

        builder.Property(details => details.TripId)
            .IsRequired()
            .HasColumnName("trip_id");

        builder.Property(details => details.From)
            .HasConversion(LocationPointConverter.Required)
            .HasColumnType("geography (point, 4326)")
            .HasColumnName("from_location");

        builder.Property(details => details.To)
            .HasConversion(LocationPointConverter.Required)
            .HasColumnType("geography (point, 4326)")
            .HasColumnName("to_location");

        builder.Property(details => details.FromAddress)
            .HasMaxLength(150)
            .HasColumnName("from_address");

        builder.Property(details => details.ToAddress)
            .HasMaxLength(150)
            .HasColumnName("to_address");

        builder.Property(details => details.DepartureAt)
            .HasColumnName("departure_at");

        builder.Property(details => details.TotalSeats)
            .IsRequired()
            .HasColumnName("total_seats");

        builder.Property(details => details.AvailableSeats)
            .IsRequired()
            .HasColumnName("available_seats");

        builder.Property(details => details.RequiredSeats)
            .HasColumnName("required_seats");

        builder.HasIndex(details => details.TripId)
            .IsUnique()
            .HasDatabaseName("ux_intercity_trip_details_trip_id");

        builder.HasIndex(details => details.AvailableSeats)
            .HasDatabaseName("ix_intercity_trip_details_available_seats");

        builder.HasIndex(details => details.DepartureAt)
            .HasDatabaseName("ix_intercity_trip_details_departure_at");

        builder.HasIndex(details => details.From)
            .HasMethod("gist")
            .HasDatabaseName("ix_intercity_trip_details_from_location_gist");

        builder.HasIndex(details => details.To)
            .HasMethod("gist")
            .HasDatabaseName("ix_intercity_trip_details_to_location_gist");
    }
}