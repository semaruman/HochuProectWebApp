using Microsoft.AspNetCore.Mvc;

namespace Web.Common.Results;

public static class ResultHttpExtensions
{
    public static IResult ToHttpResult(this Result result, Func<IResult> onSuccess)
        => result.IsSuccess ? onSuccess() : result.Error.ToProblemResult();

    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult> onSuccess)
        => result.IsSuccess ? onSuccess(result.Value) : result.Error.ToProblemResult();

    public static IResult ToProblemResult(this AppError error)
    {
        var problem = new ProblemDetails
        {
            Status = error.StatusCode,
            Title = error.ResolvedTitle,
            Detail = error.Message
        };

        if (error.ValidationErrors is not null)
            problem.Extensions["errors"] = error.ValidationErrors;

        return Microsoft.AspNetCore.Http.Results.Problem(problem);
    }
}
