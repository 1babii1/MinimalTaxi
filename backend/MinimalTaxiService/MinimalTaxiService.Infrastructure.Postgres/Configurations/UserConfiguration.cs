using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MinimalTaxiService.Domain.Entities;
using MinimalTaxiService.Domain.Enums;
using Shared;

namespace MinimalTaxiService.Infrastructure.Postgres.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id).HasName("pk_users");

        builder.Property(user => user.Id)
            .HasColumnName("id");

        builder.Property(user => user.DisplayName)
            .IsRequired()
            .HasMaxLength(LenghtConstants.LENGTH100)
            .HasColumnName("display_name");

        builder.Property(user => user.PhoneNumber)
            .HasMaxLength(LenghtConstants.LENGTH20)
            .HasColumnName("phone_number");

        builder.Property(user => user.AvatarUrl)
            .HasMaxLength(LenghtConstants.LENGTH500)
            .HasColumnName("avatar_url");

        builder.Property(user => user.Role)
            .HasConversion(role => role.ToString(), value => Enum.Parse<UserRole>(value))
            .HasMaxLength(LenghtConstants.LENGTH20)
            .IsRequired()
            .HasColumnName("role");

        builder.OwnsOne(user => user.Address, addressBuilder =>
        {
            addressBuilder.Property(address => address.City)
                .HasMaxLength(LenghtConstants.LENGTH100)
                .HasColumnName("city");

            addressBuilder.Property(address => address.Street)
                .HasMaxLength(LenghtConstants.LENGTH150)
                .HasColumnName("street");

            addressBuilder.Property(address => address.House)
                .HasMaxLength(LenghtConstants.LENGTH20)
                .HasColumnName("house");

            addressBuilder.Property(address => address.Apartment)
                .HasMaxLength(LenghtConstants.LENGTH20)
                .HasColumnName("apartment");
        });

        builder.OwnsOne(user => user.CarInfo, carInfoBuilder =>
        {
            carInfoBuilder.Property(carInfo => carInfo.Brand)
                .HasMaxLength(LenghtConstants.LENGTH50)
                .HasColumnName("car_brand");

            carInfoBuilder.Property(carInfo => carInfo.Model)
                .HasMaxLength(LenghtConstants.LENGTH50)
                .HasColumnName("car_model");

            carInfoBuilder.Property(carInfo => carInfo.Color)
                .HasMaxLength(LenghtConstants.LENGTH30)
                .HasColumnName("car_color");

            carInfoBuilder.Property(carInfo => carInfo.PlateNumber)
                .HasMaxLength(LenghtConstants.LENGTH20)
                .HasColumnName("car_plate_number");
        });

        builder.Property(user => user.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(user => user.UpdatedAt)
            .IsRequired(false)
            .HasColumnName("updated_at");

        builder.HasIndex(user => user.Role)
            .HasDatabaseName("ix_users_role");

        builder.HasIndex(user => user.PhoneNumber)
            .HasDatabaseName("ix_users_phone_number");

        builder.HasIndex(user => user.CreatedAt)
            .HasDatabaseName("ix_users_created_at");
    }
}