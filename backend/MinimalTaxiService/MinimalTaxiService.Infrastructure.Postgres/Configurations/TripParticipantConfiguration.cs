using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MinimalTaxiService.Domain.Entities;

namespace MinimalTaxiService.Infrastructure.Postgres.Configurations;

public class TripParticipantConfiguration : IEntityTypeConfiguration<TripParticipant>
{
    public void Configure(EntityTypeBuilder<TripParticipant> builder)
    {
        builder.ToTable("trip_participants");

        builder.HasKey(tripParticipant => tripParticipant.Id).HasName("pk_trip_participants");

        builder.Property(tripParticipant => tripParticipant.Id)
            .HasColumnName("id");

        builder.Property(tripParticipant => tripParticipant.TripId)
            .IsRequired()
            .HasColumnName("trip_id");

        builder.Property(tripParticipant => tripParticipant.UserId)
            .IsRequired()
            .HasColumnName("user_id");

        builder.Property(tripParticipant => tripParticipant.IsDriver)
            .IsRequired()
            .HasColumnName("is_driver");

        builder.Property(tripParticipant => tripParticipant.BookedSeats)
            .IsRequired()
            .HasColumnName("booked_seats");

        builder.Property(tripParticipant => tripParticipant.PickupLocation)
            .HasConversion(LocationPointConverter.Nullable)
            .HasColumnType("geography (point, 4326)")
            .HasColumnName("pickup_location");

        builder.Property(tripParticipant => tripParticipant.DropoffLocation)
            .HasConversion(LocationPointConverter.Nullable)
            .HasColumnType("geography (point, 4326)")
            .HasColumnName("dropoff_location");

        builder.Property(tripParticipant => tripParticipant.PickupAddress)
            .HasMaxLength(150)
            .HasColumnName("pickup_address");

        builder.Property(tripParticipant => tripParticipant.DropoffAddress)
            .HasMaxLength(150)
            .HasColumnName("dropoff_address");

        builder.HasIndex(tripParticipant => tripParticipant.TripId)
            .HasDatabaseName("ix_trip_participants_trip_id");

        builder.HasIndex(tripParticipant => new { tripParticipant.TripId, tripParticipant.UserId })
            .IsUnique()
            .HasDatabaseName("ux_trip_participants_trip_id_user_id");

        builder.HasIndex(tripParticipant => new { tripParticipant.TripId, tripParticipant.IsDriver })
            .HasDatabaseName("ix_trip_participants_trip_id_is_driver");
    }
}