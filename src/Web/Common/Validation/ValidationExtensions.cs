using FluentValidation;

namespace Web.Common.Validation;

public static class ValidationExtensions
{
    public static async Task ValidateOrThrowAsync<T>(this IValidator<T> validator, T instance, CancellationToken ct = default)
    {
        var result = await validator.ValidateAsync(instance, ct);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);
    }
}
