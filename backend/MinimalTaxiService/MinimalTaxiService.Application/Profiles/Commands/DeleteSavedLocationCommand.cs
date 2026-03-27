using CSharpFunctionalExtensions;
using FluentValidation;
using FluentValidation.Results;
using MinimalTaxiService.Application.Database;
using MinimalTaxiService.Application.Validation;
using Microsoft.Extensions.Logging;
using Shared;

namespace MinimalTaxiService.Application.Profiles.Commands;

public sealed record DeleteSavedLocationCommand(Guid UserId, Guid SavedLocationId);

public sealed class DeleteSavedLocationValidation : AbstractValidator<DeleteSavedLocationCommand>
{
    public DeleteSavedLocationValidation()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");

        RuleFor(x => x.SavedLocationId)
            .NotEmpty()
            .WithMessage("SavedLocationId is required");
    }
}

public sealed class DeleteSavedLocationCommandHandler(
    ISavedLocationRepository savedLocationRepository,
    ITransactionManager transactionManager,
    DeleteSavedLocationValidation validator,
    ILogger<DeleteSavedLocationCommandHandler> logger)
{
    public async Task<UnitResult<Error>> Handle(DeleteSavedLocationCommand command, CancellationToken cancellationToken)
    {
        ValidationResult validateResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validateResult.IsValid)
        {
            logger.LogError("Failed to validate delete saved location command");
            return validateResult.ToError();
        }

        var beginTransaction = await transactionManager.BeginTransactionAsync(cancellationToken);
        if (beginTransaction.IsFailure)
            return beginTransaction.Error;

        using var scope = beginTransaction.Value;

        var location = await savedLocationRepository.GetByIdWithLock(command.SavedLocationId, cancellationToken);
        if (location is null || location.UserId != command.UserId)
        {
            scope.Rollback();
            return Error.NotFound("saved_location.not_found", "Saved location is not found");
        }

        savedLocationRepository.Remove(location);

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            scope.Rollback();
            return saveResult.Error;
        }

        var commitResult = scope.Commit();
        if (commitResult.IsFailure)
            return commitResult.Error;

        return UnitResult.Success<Error>();
    }
}
