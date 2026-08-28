namespace Web.Common.Results;

public static class ResultErrors
{
    public static AppError NotFound(string message = "Resource not found.")
        => new(ErrorKind.NotFound, message);

    public static AppError Forbidden(string message = "Forbidden.")
        => new(ErrorKind.Forbidden, message);

    public static AppError Conflict(string message)
        => new(ErrorKind.Conflict, message);

    public static AppError Business(string message)
        => new(ErrorKind.Business, message);

    public static AppError BadRequest(string message)
        => new(ErrorKind.BadRequest, message);

    public static AppError Unauthorized(string message = "Authentication required.")
        => new(ErrorKind.Unauthorized, message);

    public static AppError Validation(IReadOnlyDictionary<string, string[]> fieldErrors, string message = "Validation failed.")
        => new(ErrorKind.Validation, message, ValidationErrors: fieldErrors);
}
