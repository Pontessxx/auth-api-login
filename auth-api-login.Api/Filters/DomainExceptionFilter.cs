using auth_api_login.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;

namespace auth_api_login.Api.Filters;

public class DomainExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not DomainException exception)
        {
            return;
        }

        var statusCode = exception switch
        {
            EmailAlreadyExistsException => StatusCodes.Status409Conflict,
            InvalidCredentialsException => StatusCodes.Status401Unauthorized,
            UserNotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };

        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = statusCode,
            Title = exception.Message
        })
        {
            StatusCode = statusCode
        };
        context.ExceptionHandled = true;
    }
}
