using FluentValidation;
using FluentValidation.Results;
using MinimalTaxiService.Application.Database;
using MinimalTaxiService.Contracts.Profiles;
using Microsoft.Extensions.Logging;

namespace MinimalTaxiService.Application.Profiles.Queries;

public sealed record GetSavedLocationsQuery(Guid UserId);

public sealed class GetSavedLocationsValidation : AbstractValidator<GetSavedLocationsQuery>
{
    public GetSavedLocationsValidation()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");
    }
}

public sealed class GetSavedLocationsQueryHandler(
    ISavedLocationRepository savedLocationRepository,
    GetSavedLocationsValidation validator,
    ILogger<GetSavedLocationsQueryHandler> logger)
{
    public async Task<IReadOnlyList<SavedLocationDto>> Handle(GetSavedLocationsQuery query, CancellationToken cancellationToken)
    {
        ValidationResult validateResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validateResult.IsValid)
        {
            logger.LogError("Failed to validate get saved locations query");
            return [];
        }

        var locations = await savedLocationRepository.GetByUserId(query.UserId, cancellationToken);

        return locations
            .Select(location => new SavedLocationDto
            {
                Id = location.Id,
                Name = location.Name,
                Address = location.Address,
                Latitude = location.Location.Latitude,
                Longitude = location.Location.Longitude,
                CreatedAt = location.CreatedAt,
            })
            .ToArray();
    }
}
