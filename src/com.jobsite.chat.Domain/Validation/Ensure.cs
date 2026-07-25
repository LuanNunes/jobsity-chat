using com.jobsite.chat.Domain.Exceptions;

namespace com.jobsite.chat.Domain.Validation;

// Guard-clause helper; the single validation mechanism. Fails fast with a
// DomainException and returns the (normalized) value so callers can one-line guards.
public static class Ensure
{
    // Throws on null/whitespace; returns the trimmed value.
    public static string NotNullOrWhiteSpace(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{name} must not be empty.");
        }

        return value.Trim();
    }

    // Throws when value.Length > maxLength; returns the value unchanged.
    public static string MaxLength(string value, int maxLength, string name)
    {
        if (value.Length > maxLength)
        {
            throw new DomainException($"{name} must not exceed {maxLength} characters.");
        }

        return value;
    }

    // Throws on Guid.Empty; returns the value unchanged.
    public static Guid NotEmpty(Guid value, string name)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"{name} must not be empty.");
        }

        return value;
    }

    // Throws on null; returns the instance.
    public static T NotNull<T>(T? value, string name) where T : class
    {
        if (value is null)
        {
            throw new DomainException($"{name} must not be null.");
        }

        return value;
    }
}
