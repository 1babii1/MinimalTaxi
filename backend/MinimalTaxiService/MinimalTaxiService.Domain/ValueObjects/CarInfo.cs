using CSharpFunctionalExtensions;
using System.Text.RegularExpressions;
using Shared;

namespace MinimalTaxiService.Domain.ValueObjects;

public record CarInfo
{
    private static readonly Regex PlateRegex = new(
        "^[ABEKMHOPCTYX]\\d{3}[ABEKMHOPCTYX]{2}\\d{2,3}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Dictionary<char, char> CyrillicToLatin = new()
    {
        ['А'] = 'A',
        ['В'] = 'B',
        ['Е'] = 'E',
        ['К'] = 'K',
        ['М'] = 'M',
        ['Н'] = 'H',
        ['О'] = 'O',
        ['Р'] = 'P',
        ['С'] = 'C',
        ['Т'] = 'T',
        ['У'] = 'Y',
        ['Х'] = 'X'
    };

    public string Brand { get; }
    public string Model { get; }
    public string Color { get; }
    public string PlateNumber { get; }

    private CarInfo(string brand, string model, string color, string plateNumber)
    {
        Brand = brand;
        Model = model;
        Color = color;
        PlateNumber = plateNumber;
    }

    public static Result<CarInfo, Error> Create(string brand, string model, string color, string plateNumber)
    {
        var errors = new List<ErrorMessages>();

        var normalizedBrand = brand?.Trim();
        var normalizedModel = model?.Trim();
        var normalizedColor = color?.Trim();
        var normalizedPlateNumber = NormalizePlateNumber(plateNumber);

        if (string.IsNullOrWhiteSpace(normalizedBrand) || normalizedBrand.Length < LenghtConstants.LENGTH2)
            errors.Add(new ErrorMessages("length.is.invalid", $"Car brand cannot be less than {LenghtConstants.LENGTH2} characters", nameof(brand)));
        else if (normalizedBrand.Length > LenghtConstants.LENGTH50)
            errors.Add(new ErrorMessages("length.is.invalid", $"Car brand cannot be greater than {LenghtConstants.LENGTH50} characters", nameof(brand)));

        if (string.IsNullOrWhiteSpace(normalizedModel) || normalizedModel.Length < LenghtConstants.LENGTH2)
            errors.Add(new ErrorMessages("length.is.invalid", $"Car model cannot be less than {LenghtConstants.LENGTH2} characters", nameof(model)));
        else if (normalizedModel.Length > LenghtConstants.LENGTH50)
            errors.Add(new ErrorMessages("length.is.invalid", $"Car model cannot be greater than {LenghtConstants.LENGTH50} characters", nameof(model)));

        if (string.IsNullOrWhiteSpace(normalizedColor) || normalizedColor.Length < LenghtConstants.LENGTH2)
            errors.Add(new ErrorMessages("length.is.invalid", $"Car color cannot be less than {LenghtConstants.LENGTH2} characters", nameof(color)));
        else if (normalizedColor.Length > LenghtConstants.LENGTH30)
            errors.Add(new ErrorMessages("length.is.invalid", $"Car color cannot be greater than {LenghtConstants.LENGTH30} characters", nameof(color)));

        if (string.IsNullOrWhiteSpace(normalizedPlateNumber))
            errors.Add(new ErrorMessages("value.is.required", "Plate number is required", nameof(plateNumber)));
        else if (!PlateRegex.IsMatch(normalizedPlateNumber))
            errors.Add(new ErrorMessages("value.is.invalid", "Plate number must match format M365MH102", nameof(plateNumber)));

        if (errors.Any())
            return Result.Failure<CarInfo, Error>(Error.Validation(errors));

        return Result.Success<CarInfo, Error>(new CarInfo(
            normalizedBrand!,
            normalizedModel!,
            normalizedColor!,
            normalizedPlateNumber!));
    }

    private static string? NormalizePlateNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var source = value.Trim().ToUpperInvariant();
        var normalizedChars = new List<char>(source.Length);

        foreach (var character in source)
        {
            if (character is ' ' or '-')
                continue;

            if (CyrillicToLatin.TryGetValue(character, out var latinCharacter))
            {
                normalizedChars.Add(latinCharacter);
                continue;
            }

            if (char.IsLetterOrDigit(character))
                normalizedChars.Add(character);
        }

        return normalizedChars.Count == 0 ? null : new string(normalizedChars.ToArray());
    }
}