using com.jobsite.chat.Domain.Exceptions;
using FluentValidation;
using FluentValidation.Results;

namespace com.jobsite.chat.Domain.Rules;

public static class DomainValidation
{
    public static void ValidateAndThrowDomain<T>(this IValidator<T> validator, T instance)
    {
        ValidationResult result = validator.Validate(instance);
        if (!result.IsValid)
        {
            throw new DomainException(
                string.Join(" ", result.Errors.Select(e => e.ErrorMessage)));
        }
    }
}
