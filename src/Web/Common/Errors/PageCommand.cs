using Microsoft.AspNetCore.Mvc;
using Web.Common.Results;

namespace Web.Common.Errors;

public static class PageCommand
{
    public static async Task<IActionResult> ExecuteAsync(
        Func<Task<IActionResult>> onSuccess,
        Func<AppError, Task<IActionResult>> onError)
    {
        return await onSuccess();
    }

    public static async Task<IActionResult> FromResultAsync(
        Result result,
        Func<Task<IActionResult>> onSuccess,
        Func<string, Task<IActionResult>> onBusinessError)
    {
        if (result.IsFailure)
            return await onBusinessError(result.Error.Message);
        return await onSuccess();
    }

    public static async Task<IActionResult> FromResultAsync<T>(
        Result<T> result,
        Func<T, Task<IActionResult>> onSuccess,
        Func<string, Task<IActionResult>> onBusinessError)
    {
        if (result.IsFailure)
            return await onBusinessError(result.Error.Message);
        return await onSuccess(result.Value);
    }
}
