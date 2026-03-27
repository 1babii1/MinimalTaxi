using FluentValidation;
using FluentValidation.Results;
using MinimalTaxiService.Application.Database;
using MinimalTaxiService.Application.Validation;
using MinimalTaxiService.Contracts.Trips;
using MinimalTaxiService.Domain.Enums;
using MinimalTaxiService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Shared;

namespace MinimalTaxiService.Application.Trips.Queries;

public sealed record GetNearbyTripsQuery(
    double Latitude,
    double Longitude,
    int RadiusMeters,
    string? City,
    string? FromAddress,
    string? ToAddress,
    double? FromLatitude,
    double? FromLongitude,
    int? FromRadiusMeters,
    double? ToLatitude,
    double? ToLongitude,
    int? ToRadiusMeters,
    TripType? TripType,
    int Limit = 20,
    int Offset = 0,
    bool IncludeInactive = false);

public sealed class GetNearbyTripsValidation : AbstractValidator<GetNearbyTripsQuery>
{
    public GetNearbyTripsValidation()
    {
        RuleFor(x => x.RadiusMeters)
            .GreaterThan(0)
            .WithMessage("RadiusMeters must be greater than zero");

        RuleFor(x => x.Limit)
            .GreaterThan(0).WithMessage("Limit must be greater than zero")
            .LessThanOrEqualTo(20).WithMessage("Limit must be less than or equal to 20");

        RuleFor(x => x.Offset)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Offset must be greater than or equal to zero");

        RuleFor(x => x.City)
            .MaximumLength(LenghtConstants.LENGTH100)
            .WithMessage($"City max length is {LenghtConstants.LENGTH100}")
            .When(x => !string.IsNullOrWhiteSpace(x.City));

        RuleFor(x => x.FromAddress)
            .MaximumLength(LenghtConstants.LENGTH150)
            .WithMessage($"FromAddress max length is {LenghtConstants.LENGTH150}")
            .When(x => !string.IsNullOrWhiteSpace(x.FromAddress));

        RuleFor(x => x.ToAddress)
            .MaximumLength(LenghtConstants.LENGTH150)
            .WithMessage($"ToAddress max length is {LenghtConstants.LENGTH150}")
            .When(x => !string.IsNullOrWhiteSpace(x.ToAddress));

        RuleFor(x => x)
            .Must(query =>
            {
                var hasFromLatitude = query.FromLatitude.HasValue;
                var hasFromLongitude = query.FromLongitude.HasValue;
                var hasFromRadius = query.FromRadiusMeters.HasValue;

                return (hasFromLatitude == hasFromLongitude) &&
                       (!hasFromLatitude || hasFromRadius);
            })
            .WithMessage("From point filter requires fromLatitude, fromLongitude and fromRadiusMeters");

        RuleFor(x => x)
            .Must(query =>
            {
                var hasToLatitude = query.ToLatitude.HasValue;
                var hasToLongitude = query.ToLongitude.HasValue;
                var hasToRadius = query.ToRadiusMeters.HasValue;

                return (hasToLatitude == hasToLongitude) &&
                       (!hasToLatitude || hasToRadius);
            })
            .WithMessage("To point filter requires toLatitude, toLongitude and toRadiusMeters");

        RuleFor(x => x.FromRadiusMeters)
            .GreaterThan(0)
            .WithMessage("FromRadiusMeters must be greater than zero")
            .When(x => x.FromRadiusMeters.HasValue);

        RuleFor(x => x.ToRadiusMeters)
            .GreaterThan(0)
            .WithMessage("ToRadiusMeters must be greater than zero")
            .When(x => x.ToRadiusMeters.HasValue);

        RuleFor(x => x)
            .Must(query =>
                !query.FromLatitude.HasValue ||
                !query.FromLongitude.HasValue ||
                Location.Create(query.FromLatitude.Value, query.FromLongitude.Value).IsSuccess)
            .WithMessage("From point location is invalid");

        RuleFor(x => x)
            .Must(query =>
                !query.ToLatitude.HasValue ||
                !query.ToLongitude.HasValue ||
                Location.Create(query.ToLatitude.Value, query.ToLongitude.Value).IsSuccess)
            .WithMessage("To point location is invalid");

        RuleFor(x => x)
            .Must(query => Location.Create(query.Latitude, query.Longitude).IsSuccess)
            .WithMessage("Location is invalid");
    }
}

public sealed class GetNearbyTripsQueryHandler(
    ITripReadRepository readRepository,
    GetNearbyTripsValidation validator,
    ILogger<GetNearbyTripsQueryHandler> logger)
{
    public async Task<IReadOnlyList<NearbyTripDto>> Handle(GetNearbyTripsQuery query, CancellationToken cancellationToken)
    {
        ValidationResult validateResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validateResult.IsValid)
        {
            logger.LogError("Failed to validate get nearby trips query");
            return [];
        }

        var limit = query.Limit <= 0 ? 20 : Math.Min(query.Limit, 20);
        var offset = query.Offset < 0 ? 0 : query.Offset;

        return await readRepository.GetNearbyTrips(
            query.Latitude,
            query.Longitude,
            query.RadiusMeters,
            query.City,
            query.FromAddress,
            query.ToAddress,
            query.FromLatitude,
            query.FromLongitude,
            query.FromRadiusMeters,
            query.ToLatitude,
            query.ToLongitude,
            query.ToRadiusMeters,
            query.TripType,
            limit,
            offset,
            query.IncludeInactive,
            cancellationToken);
    }
}
