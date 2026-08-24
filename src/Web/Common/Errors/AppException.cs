namespace Web.Common.Errors;

public class AppException : Exception
{
    public int StatusCode { get; }
    public string? Type { get; }
    public string? Title { get; }

    public AppException(int statusCode, string message, string? title = null, string? type = null)
        : base(message)
    {
        StatusCode = statusCode;
        Title = title ?? message;
        Type = type;
    }
}

public static class AppErrors
{
    public static AppException NotFound(string message = "Resource not found.")
        => new(StatusCodes.Status404NotFound, message, "Not Found");

    public static AppException Forbidden(string message = "Forbidden.")
        => new(StatusCodes.Status403Forbidden, message, "Forbidden");

    public static AppException Conflict(string message)
        => new(StatusCodes.Status409Conflict, message, "Conflict");

    public static AppException Business(string message)
        => new(StatusCodes.Status422UnprocessableEntity, message, "Business Rule Violation");

    public static AppException BadRequest(string message)
        => new(StatusCodes.Status400BadRequest, message, "Bad Request");
}
