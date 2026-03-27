using FluentValidation;
using FluentValidation.Results;
using MinimalTaxiService.Application.Database;
using MinimalTaxiService.Contracts.Trips;
using Microsoft.Extensions.Logging;

namespace MinimalTaxiService.Application.Trips.Queries;

public sealed record GetUserTripsQuery(Guid UserId, bool OnlyActive = false);

public sealed class GetUserTripsValidation : AbstractValidator<GetUserTripsQuery>
{
    public GetUserTripsValidation()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");
    }
}

public sealed class GetUserTripsQueryHandler(
    IUserTripsReadRepository readRepository,
    GetUserTripsValidation validator,
    ILogger<GetUserTripsQueryHandler> logger)
{
    public async Task<IReadOnlyList<UserTripDto>> Handle(GetUserTripsQuery query, CancellationToken cancellationToken)
    {
        ValidationResult validateResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validateResult.IsValid)
        {
            logger.LogError("Failed to validate get user trips query");
            return [];
        }

        return await readRepository.GetUserTrips(query.UserId, query.OnlyActive, cancellationToken);
    }
}
