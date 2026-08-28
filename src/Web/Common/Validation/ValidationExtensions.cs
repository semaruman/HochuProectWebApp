using FluentValidation;
using Web.Common.Results;

namespace Web.Common.Validation;

public static class ValidationExtensions
{
    public static async Task<Result> ValidateRequestAsync<T>(this IValidator<T> validator, T instance, CancellationToken ct = default)
    {
        var result = await validator.ValidateAsync(instance, ct);
        if (result.IsValid)
            return Result.Success();

        var errors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

        return ResultErrors.Validation(errors);
    }
}
