using System.Text.RegularExpressions;

namespace AuthService.Domain.ValueObjects;

public sealed class Email
{
    private static readonly Regex EmailRegex =
        new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static bool TryCreate(string? rawValue, out Email? email, out string? error)
    {
        email = null;
        error = null;

        var normalized = rawValue?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            error = "Email is required.";
            return false;
        }

        if (normalized.Length > 256)
        {
            error = "Email is too long.";
            return false;
        }

        if (!EmailRegex.IsMatch(normalized))
        {
            error = "Email format is invalid.";
            return false;
        }

        email = new Email(normalized);
        return true;
    }
}
