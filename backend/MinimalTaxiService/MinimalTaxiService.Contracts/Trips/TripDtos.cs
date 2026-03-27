namespace MinimalTaxiService.Contracts.Trips;

public sealed class GeoPointDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public sealed class CreateLocalTripRequest
{
    public GeoPointDto From { get; set; } = new();
    public GeoPointDto To { get; set; } = new();
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class CreateIntercityTripRequest
{
    public GeoPointDto From { get; set; } = new();
    public GeoPointDto To { get; set; } = new();
    public string? FromAddress { get; set; }
    public string? ToAddress { get; set; }
    public DateTimeOffset DepartureAt { get; set; }
    public string? Description { get; set; }
    public bool CreatedByDriver { get; set; }
    public int? TotalSeats { get; set; }
    public int? RequiredSeats { get; set; }
}

public sealed class AcceptTripRequest
{
    public int? TotalSeats { get; set; }
}

public sealed class CancelTripRequest
{
    public string Reason { get; set; } = string.Empty;
}

public sealed class JoinIntercityTripRequest
{
    public int Seats { get; set; }
    public GeoPointDto Pickup { get; set; } = new();
    public GeoPointDto Dropoff { get; set; } = new();
    public string PickupAddress { get; set; } = string.Empty;
    public string DropoffAddress { get; set; } = string.Empty;
}

public sealed class CreateIntercityInvitationRequest
{
    public Guid PassengerTripId { get; set; }
    public Guid DriverTripId { get; set; }
}

public sealed class IntercityInvitationDto
{
    public Guid InvitationId { get; set; }
    public Guid PassengerTripId { get; set; }
    public Guid DriverTripId { get; set; }
    public Guid DriverId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

public sealed class NearbyTripDto
{
    public Guid TripId { get; set; }
    public string TripType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid PassengerId { get; set; }
    public Guid? DriverId { get; set; }
    public string? PassengerName { get; set; }
    public string? PassengerPhone { get; set; }
    public bool IsPassengerRequest { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? DestinationLatitude { get; set; }
    public double? DestinationLongitude { get; set; }
    public string? City { get; set; }
    public string? FromAddress { get; set; }
    public string? ToAddress { get; set; }
    public DateTimeOffset? DepartureAt { get; set; }
    public string? DriverName { get; set; }
    public string? DriverPhone { get; set; }
    public string? DriverCarBrand { get; set; }
    public string? DriverCarModel { get; set; }
    public string? DriverCarColor { get; set; }
    public string? DriverCarPlateNumber { get; set; }
    public int? TotalSeats { get; set; }
    public int? AvailableSeats { get; set; }
    public int? RequiredSeats { get; set; }
    public double DistanceMeters { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class UserTripDto
{
    public Guid TripId { get; set; }
    public string TripType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool CreatedByUser { get; set; }
    public Guid PassengerId { get; set; }
    public string? City { get; set; }
    public string? FromAddress { get; set; }
    public string? ToAddress { get; set; }
    public double? OriginLatitude { get; set; }
    public double? OriginLongitude { get; set; }
    public double? DestinationLatitude { get; set; }
    public double? DestinationLongitude { get; set; }
    public Guid? DriverId { get; set; }
    public string? PassengerName { get; set; }
    public string? PassengerPhone { get; set; }
    public string? DriverName { get; set; }
    public string? DriverPhone { get; set; }
    public string? DriverCarBrand { get; set; }
    public string? DriverCarModel { get; set; }
    public string? DriverCarColor { get; set; }
    public string? DriverCarPlateNumber { get; set; }
    public int? TotalSeats { get; set; }
    public int? AvailableSeats { get; set; }
    public int? RequiredSeats { get; set; }
    public DateTimeOffset? DepartureAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class IntercityPassengerDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int Seats { get; set; }
    public string? PickupAddress { get; set; }
    public string? DropoffAddress { get; set; }
    public double? DistanceMetersFromOrigin { get; set; }
}
