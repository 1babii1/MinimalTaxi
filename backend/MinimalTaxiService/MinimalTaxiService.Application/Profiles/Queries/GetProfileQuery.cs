using FluentValidation;
using FluentValidation.Results;
using MinimalTaxiService.Application.Database;
using MinimalTaxiService.Contracts.Profiles;
using Microsoft.Extensions.Logging;

namespace MinimalTaxiService.Application.Profiles.Queries;

public sealed record GetProfileQuery(Guid UserId);

public sealed class GetProfileValidation : AbstractValidator<GetProfileQuery>
{
    public GetProfileValidation()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");
    }
}

public sealed class GetProfileQueryHandler(
    IProfileReadRepository readRepository,
    GetProfileValidation validator,
    ILogger<GetProfileQueryHandler> logger)
{
    public async Task<ProfileDto?> Handle(GetProfileQuery query, CancellationToken cancellationToken)
    {
        ValidationResult validateResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validateResult.IsValid)
        {
            logger.LogError("Failed to validate get profile query");
            return null;
        }

        return await readRepository.GetProfile(query.UserId, cancellationToken);
    }
}
