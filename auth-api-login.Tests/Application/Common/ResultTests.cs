namespace auth_api_login.Tests.Application.Common;

public class ResultTests
{
    [Fact]
    public void Success_IsSuccessAndHasNoError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Theory]
    [InlineData(ResultErrorType.Conflict, "conflict")]
    [InlineData(ResultErrorType.Unauthorized, "unauthorized")]
    [InlineData(ResultErrorType.NotFound, "not found")]
    public void FailureFactories_SetExpectedErrorTypeAndMessage(ResultErrorType type, string message)
    {
        var result = type switch
        {
            ResultErrorType.Conflict => Result.Conflict(message),
            ResultErrorType.Unauthorized => Result.Unauthorized(message),
            ResultErrorType.NotFound => Result.NotFound(message),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(type, result.Error!.Value.Type);
        Assert.Equal(message, result.Error.Value.Message);
    }

    [Fact]
    public void GenericSuccess_ExposesValue()
    {
        var result = Result<string>.Success("value");

        Assert.True(result.IsSuccess);
        Assert.Equal("value", result.Value);
    }

    [Fact]
    public void GenericFailure_AccessingValueThrows()
    {
        var result = Result<string>.NotFound("missing");

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.NotFound, result.Error!.Value.Type);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }
}
