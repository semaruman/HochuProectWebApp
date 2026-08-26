using Microsoft.AspNetCore.Mvc;
using Web.Domain.Exceptions;

namespace Web.Common.Errors;

public static class PageCommand
{
    public static async Task<IActionResult> ExecuteAsync(
        Func<Task<IActionResult>> onSuccess,
        Func<string, Task<IActionResult>> onBusinessError)
    {
        try
        {
            return await onSuccess();
        }
        catch (DomainException ex)
        {
            return await onBusinessError(ex.Message);
        }
        catch (AppException ex) when (ex.StatusCode is StatusCodes.Status400BadRequest
            or StatusCodes.Status409Conflict
            or StatusCodes.Status422UnprocessableEntity)
        {
            return await onBusinessError(ex.Message);
        }
        catch (AppException ex) when (ex.StatusCode == StatusCodes.Status404NotFound)
        {
            return new NotFoundResult();
        }
        catch (AppException ex) when (ex.StatusCode == StatusCodes.Status403Forbidden)
        {
            return new ForbidResult();
        }
    }
}
