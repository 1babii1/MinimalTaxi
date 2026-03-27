using CSharpFunctionalExtensions;
using FluentValidation;
using FluentValidation.Results;
using MinimalTaxiService.Application.Database;
using MinimalTaxiService.Application.Validation;
using MinimalTaxiService.Contracts.Profiles;
using Microsoft.Extensions.Logging;
using Shared;

namespace MinimalTaxiService.Application.Profiles.Commands;

public sealed record CreateSavedLocationCommand(
    Guid UserId,
    string Name,
    string Address,
    double Latitude,
    double Longitude);

public sealed class CreateSavedLocationValidation : AbstractValidator<CreateSavedLocationCommand>
{
    public CreateSavedLocationValidation()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(LenghtConstants.LENGTH120).WithMessage($"Name max length is {LenghtConstants.LENGTH120}");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required")
            .MaximumLength(LenghtConstants.LENGTH500).WithMessage($"Address max length is {LenghtConstants.LENGTH500}");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("Latitude must be in range [-90, 90]");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("Longitude must be in range [-180, 180]");
    }
}

public sealed class CreateSavedLocationCommandHandler(
    ISavedLocationRepository savedLocationRepository,
    ITransactionManager transactionManager,
    CreateSavedLocationValidation validator,
    ILogger<CreateSavedLocationCommandHandler> logger)
{
    public async Task<Result<SavedLocationDto, Error>> Handle(CreateSavedLocationCommand command, CancellationToken cancellationToken)
    {
        ValidationResult validateResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validateResult.IsValid)
        {
            logger.LogError("Failed to validate create saved location command");
            return validateResult.ToError();
        }

        var beginTransaction = await transactionManager.BeginTransactionAsync(cancellationToken);
        if (beginTransaction.IsFailure)
            return beginTransaction.Error;

        using var scope = beginTransaction.Value;

        var createResult = Domain.Entities.SavedLocation.Create(
            command.UserId,
            command.Name,
            command.Address,
            command.Latitude,
            command.Longitude);

        if (createResult.IsFailure)
        {
            scope.Rollback();
            return createResult.Error;
        }

        await savedLocationRepository.Add(createResult.Value, cancellationToken);

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            scope.Rollback();
            return saveResult.Error;
        }

        var commitResult = scope.Commit();
        if (commitResult.IsFailure)
            return commitResult.Error;

        return new SavedLocationDto
        {
            Id = createResult.Value.Id,
            Name = createResult.Value.Name,
            Address = createResult.Value.Address,
            Latitude = createResult.Value.Location.Latitude,
            Longitude = createResult.Value.Location.Longitude,
            CreatedAt = createResult.Value.CreatedAt,
        };
    }
}
