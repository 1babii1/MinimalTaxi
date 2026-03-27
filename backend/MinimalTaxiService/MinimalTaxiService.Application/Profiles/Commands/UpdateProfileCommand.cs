using CSharpFunctionalExtensions;
using FluentValidation;
using FluentValidation.Results;
using System.Text.RegularExpressions;
using MinimalTaxiService.Application.Database;
using MinimalTaxiService.Application.Validation;
using MinimalTaxiService.Contracts.Profiles;
using MinimalTaxiService.Domain.Enums;
using MinimalTaxiService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Shared;

namespace MinimalTaxiService.Application.Profiles.Commands;

public sealed record UpdateProfileCommand(
    Guid UserId,
    UserRole Role,
    string Name,
    string? Phone,
    AddressDto? Address,
    CarInfoDto? CarInfo);

public sealed class UpdateProfileValidation : AbstractValidator<UpdateProfileCommand>
{
    private static readonly Regex PhoneRegex = new(
        "^(?:\\+7|7|8)\\d{10}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex PlateRegex = new(
        "^[ABEKMHOPCTYXАВЕКМНОРСТУХ]\\d{3}[ABEKMHOPCTYXАВЕКМНОРСТУХ]{2}\\d{2,3}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public UpdateProfileValidation()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");

        RuleFor(x => x.Role)
            .IsInEnum()
            .WithMessage("Role is invalid");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(LenghtConstants.LENGTH2).WithMessage($"Name min length is {LenghtConstants.LENGTH2}")
            .MaximumLength(LenghtConstants.LENGTH100).WithMessage($"Name max length is {LenghtConstants.LENGTH100}");

        RuleFor(x => x.Phone)
            .Must(value => value is null || PhoneRegex.IsMatch(value.Replace("-", string.Empty).Replace("(", string.Empty).Replace(")", string.Empty).Replace(" ", string.Empty)))
            .WithMessage("Phone must match +7XXXXXXXXXX")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x)
            .Must(x => x.Role != UserRole.Passenger || x.CarInfo is null)
            .WithMessage("Passenger profile must not contain CarInfo");

        RuleFor(x => x)
            .Must(x => x.Role != UserRole.Driver || x.Address is null)
            .WithMessage("Driver profile must not contain Address");

        When(x => x.Address is not null, () =>
        {
            RuleFor(x => x.Address!.City)
                .NotEmpty().WithMessage("Address.City is required")
                .MaximumLength(LenghtConstants.LENGTH100).WithMessage($"Address.City max length is {LenghtConstants.LENGTH100}");
            RuleFor(x => x.Address!.Street)
                .NotEmpty().WithMessage("Address.Street is required")
                .MaximumLength(LenghtConstants.LENGTH150).WithMessage($"Address.Street max length is {LenghtConstants.LENGTH150}");
            RuleFor(x => x.Address!.House)
                .NotEmpty().WithMessage("Address.House is required")
                .MaximumLength(LenghtConstants.LENGTH20).WithMessage($"Address.House max length is {LenghtConstants.LENGTH20}");
            RuleFor(x => x.Address!.Apartment)
                .MaximumLength(LenghtConstants.LENGTH20).WithMessage($"Address.Apartment max length is {LenghtConstants.LENGTH20}")
                .When(x => !string.IsNullOrWhiteSpace(x.Address!.Apartment));
        });

        When(x => x.CarInfo is not null, () =>
        {
            RuleFor(x => x.CarInfo!.Brand)
                .NotEmpty().WithMessage("CarInfo.Brand is required")
                .MaximumLength(LenghtConstants.LENGTH50).WithMessage($"CarInfo.Brand max length is {LenghtConstants.LENGTH50}");
            RuleFor(x => x.CarInfo!.Model)
                .NotEmpty().WithMessage("CarInfo.Model is required")
                .MaximumLength(LenghtConstants.LENGTH50).WithMessage($"CarInfo.Model max length is {LenghtConstants.LENGTH50}");
            RuleFor(x => x.CarInfo!.Color)
                .NotEmpty().WithMessage("CarInfo.Color is required")
                .MaximumLength(LenghtConstants.LENGTH30).WithMessage($"CarInfo.Color max length is {LenghtConstants.LENGTH30}");
            RuleFor(x => x.CarInfo!.PlateNumber)
                .NotEmpty().WithMessage("CarInfo.PlateNumber is required")
                .Must(value => PlateRegex.IsMatch(value.Replace("-", string.Empty).Replace(" ", string.Empty)))
                .WithMessage("CarInfo.PlateNumber must match format M365MH102");
        });
    }
}

public sealed class UpdateProfileCommandHandler(
    IUserRepository userRepository,
    ITransactionManager transactionManager,
    UpdateProfileValidation validator,
    ILogger<UpdateProfileCommandHandler> logger)
{
    public async Task<UnitResult<Error>> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        ValidationResult validateResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validateResult.IsValid)
        {
            logger.LogError("Failed to validate update profile command");
            return validateResult.ToError();
        }

        var beginTransaction = await transactionManager.BeginTransactionAsync(cancellationToken);
        if (beginTransaction.IsFailure)
            return beginTransaction.Error;

        using var scope = beginTransaction.Value;

        var user = await userRepository.GetByIdWithLock(command.UserId, cancellationToken);
        if (user is null)
        {
            var createResult = Domain.Entities.User.Create(command.UserId, command.Name, command.Role, command.Phone);
            if (createResult.IsFailure)
                return createResult.Error;

            user = createResult.Value;
            await userRepository.Add(user, cancellationToken);
        }

        var profileResult = user.UpdateProfile(command.Name, command.Phone);
        if (profileResult.IsFailure)
            return profileResult.Error;

        if (command.Role == UserRole.Passenger)
        {
            if (command.Address is not null)
            {
                var addressResult = UserAddress.Create(
                    command.Address.City,
                    command.Address.Street,
                    command.Address.House,
                    command.Address.Apartment);
                if (addressResult.IsFailure)
                    return addressResult.Error;

                var updateAddressResult = user.UpdateAddress(addressResult.Value);
                if (updateAddressResult.IsFailure)
                    return updateAddressResult.Error;
            }

            var clearCarInfoResult = user.AssignCarInfo(null);
            if (clearCarInfoResult.IsFailure)
                return clearCarInfoResult.Error;
        }
        else
        {
            if (command.CarInfo is not null)
            {
                var carInfoResult = Domain.ValueObjects.CarInfo.Create(
                    command.CarInfo.Brand,
                    command.CarInfo.Model,
                    command.CarInfo.Color,
                    command.CarInfo.PlateNumber);
                if (carInfoResult.IsFailure)
                    return carInfoResult.Error;

                var assignCarResult = user.AssignCarInfo(carInfoResult.Value);
                if (assignCarResult.IsFailure)
                    return assignCarResult.Error;
            }

            var clearAddressResult = user.UpdateAddress(null);
            if (clearAddressResult.IsFailure)
                return clearAddressResult.Error;
        }

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
