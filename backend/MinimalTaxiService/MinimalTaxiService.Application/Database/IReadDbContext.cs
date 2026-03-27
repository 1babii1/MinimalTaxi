using MinimalTaxiService.Domain.Entities;

namespace MinimalTaxiService.Application.Database;

public interface IReadDbContext
{
    IQueryable<User> UserRead { get; }

    IQueryable<Trip> TripRead { get; }

    IQueryable<TripParticipant> TripParticipantRead { get; }

    IQueryable<DriverLocation> DriverLocationRead { get; }

    IQueryable<IntercityTripDetails> IntercityTripDetailsRead { get; }
}