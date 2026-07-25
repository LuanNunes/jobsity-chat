using FluentValidation;
using FluentValidation.Results;
using com.jobsite.chat.Domain.Exceptions;

namespace com.jobsite.chat.Domain.Validation;

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
