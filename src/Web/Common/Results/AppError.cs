namespace Web.Common.Results;

public sealed record AppError(
    ErrorKind Kind,
    string Message,
    string? Title = null,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null)
{
    public int StatusCode => Kind switch
    {
        ErrorKind.BadRequest => StatusCodes.Status400BadRequest,
        ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        ErrorKind.Business => StatusCodes.Status422UnprocessableEntity,
        ErrorKind.Validation => StatusCodes.Status400BadRequest,
        _ => StatusCodes.Status500InternalServerError
    };

    public string ResolvedTitle => Title ?? Kind switch
    {
        ErrorKind.BadRequest => "Bad Request",
        ErrorKind.Unauthorized => "Unauthorized",
        ErrorKind.Forbidden => "Forbidden",
        ErrorKind.NotFound => "Not Found",
        ErrorKind.Conflict => "Conflict",
        ErrorKind.Business => "Business Rule Violation",
        ErrorKind.Validation => "Validation Error",
        _ => "Error"
    };
}
