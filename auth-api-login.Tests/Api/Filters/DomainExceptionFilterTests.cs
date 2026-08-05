using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace auth_api_login.Tests.Api.Filters;

public class DomainExceptionFilterTests
{
    private sealed class TestDomainException(string message) : DomainException(message)
    {
    }

    private static ExceptionContext CreateContext(Exception exception)
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());

        return new ExceptionContext(actionContext, [])
        {
            Exception = exception
        };
    }

    [Theory]
    [MemberData(nameof(DomainExceptionCases))]
    public void OnException_WithDomainException_SetsExpectedStatusCode(DomainException exception, int expectedStatusCode)
    {
        var context = CreateContext(exception);
        var filter = new DomainExceptionFilter();

        filter.OnException(context);

        Assert.True(context.ExceptionHandled);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(expectedStatusCode, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(expectedStatusCode, problemDetails.Status);
        Assert.Equal(exception.Message, problemDetails.Title);
    }

    public static IEnumerable<object[]> DomainExceptionCases()
    {
        yield return [new EmailAlreadyExistsException("a@test.com"), StatusCodes.Status409Conflict];
        yield return [new InvalidCredentialsException(), StatusCodes.Status401Unauthorized];
        yield return [new UserNotFoundException(Guid.NewGuid()), StatusCodes.Status404NotFound];
        yield return [new TestDomainException("generic"), StatusCodes.Status400BadRequest];
    }

    [Fact]
    public void OnException_WithNonDomainException_DoesNotHandle()
    {
        var context = CreateContext(new InvalidOperationException("boom"));
        var filter = new DomainExceptionFilter();

        filter.OnException(context);

        Assert.False(context.ExceptionHandled);
        Assert.Null(context.Result);
    }
}
