namespace auth_api_login.Api.Extensions;

public static class ResultExtensions
{
    public static ObjectResult ToProblem(this Result result)
    {
        var error = result.Error!.Value;
        var statusCode = error.Type switch
        {
            ResultErrorType.Conflict => StatusCodes.Status409Conflict,
            ResultErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ResultErrorType.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };

        return new ObjectResult(new ProblemDetails
        {
            Status = statusCode,
            Title = error.Message
        })
        {
            StatusCode = statusCode
        };
    }

    public static ObjectResult UnauthorizedProblem(string message) =>
        new(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = message
        })
        {
            StatusCode = StatusCodes.Status401Unauthorized
        };
}
