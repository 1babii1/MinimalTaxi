namespace AuthService.Domain.ValueObjects;

public sealed class Password
{
    public string Value { get; }

    private Password(string value)
    {
        Value = value;
    }

    public static bool TryCreate(string? rawValue, out Password? password, out string? error)
    {
        password = null;
        error = null;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            error = "Password is required.";
            return false;
        }

        var value = rawValue.Trim();

        if (value.Length < 8)
        {
            error = "Password must be at least 8 characters.";
            return false;
        }

        if (!value.Any(char.IsUpper))
        {
            error = "Password must contain at least one uppercase letter.";
            return false;
        }

        if (!value.Any(char.IsLower))
        {
            error = "Password must contain at least one lowercase letter.";
            return false;
        }

        if (!value.Any(char.IsDigit))
        {
            error = "Password must contain at least one digit.";
            return false;
        }

        password = new Password(value);
        return true;
    }
}
