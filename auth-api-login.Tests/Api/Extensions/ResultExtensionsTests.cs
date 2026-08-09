namespace auth_api_login.Tests.Api.Extensions;

public class ResultExtensionsTests
{
    [Theory]
    [MemberData(nameof(FailureCases))]
    public void ToProblem_MapsErrorTypeToExpectedStatusCode(Result result, int expectedStatusCode)
    {
        var objectResult = result.ToProblem();

        Assert.Equal(expectedStatusCode, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(expectedStatusCode, problemDetails.Status);
        Assert.Equal(result.Error!.Value.Message, problemDetails.Title);
    }

    public static IEnumerable<object[]> FailureCases()
    {
        yield return [Result.Conflict("já existe"), StatusCodes.Status409Conflict];
        yield return [Result.Unauthorized("não autorizado"), StatusCodes.Status401Unauthorized];
        yield return [Result.NotFound("não encontrado"), StatusCodes.Status404NotFound];
    }
}
