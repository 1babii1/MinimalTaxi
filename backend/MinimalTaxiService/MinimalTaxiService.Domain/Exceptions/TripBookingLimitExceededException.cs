using System;
using MinimalTaxiService.Domain.Enums;

namespace MinimalTaxiService.Domain.Exceptions;


public class TripBookingLimitExceededException : Exception
{
    public TripBookingLimitExceededException(int maxLimit)
        : base($"Maximum {maxLimit} concurrent active trips allowed per passenger.") { }
}

public class TripAlreadyAcceptedException : Exception
{
    public TripAlreadyAcceptedException(Guid tripId)
        : base($"Trip {tripId} already has a driver.") { }
}

public class TripCannotAcceptedException : Exception
{
    public TripCannotAcceptedException(Guid tripId, TripStatus status)
        : base($"Trip {tripId} cannot be accepted (current status: {status}).") { }
}

public class NotEnoughSeatsException : Exception
{
    public NotEnoughSeatsException(int requested, int available)
        : base($"Requested {requested} seats, but only {available} available.") { }
}
