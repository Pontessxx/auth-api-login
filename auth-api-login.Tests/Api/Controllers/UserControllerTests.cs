namespace auth_api_login.Tests.Api.Controllers;

public class UserControllerTests
{
    private readonly Mock<IUserService> _userService = new();

    private UserController CreateController(ClaimsPrincipal? user = null)
    {
        return new UserController(_userService.Object, NullLogger<UserController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user ?? new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };
    }

    private static ClaimsPrincipal BuildPrincipalWithSub(Guid userId) =>
        new(new ClaimsIdentity([new Claim("sub", userId.ToString())]));

    private static ClaimsPrincipal BuildPrincipalWithNameIdentifier(Guid userId) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())]));

    private static void AssertUnauthorizedProblem(IActionResult? result)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("Usuário precisa estar autenticado.", problem.Title);
    }

    private static UserResponse SampleResponse(Guid id) => new()
    {
        Id = id,
        Username = "john",
        Email = "john@test.com",
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task GetMe_WhenAuthorized_ReturnsOkWithUserResponse()
    {
        var userId = Guid.NewGuid();
        var response = SampleResponse(userId);
        _userService.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<UserResponse>.Success(response));

        var controller = CreateController(BuildPrincipalWithSub(userId));
        var result = await controller.GetMe(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(response, okResult.Value);
    }

    [Fact]
    public async Task GetMe_WhenSubjectMissing_ReturnsUnauthorized()
    {
        var controller = CreateController();
        var result = await controller.GetMe(CancellationToken.None);

        AssertUnauthorizedProblem(result.Result);
    }

    [Fact]
    public async Task GetMe_FallsBackToNameIdentifierClaim()
    {
        var userId = Guid.NewGuid();
        var response = SampleResponse(userId);
        _userService.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<UserResponse>.Success(response));

        var controller = CreateController(BuildPrincipalWithNameIdentifier(userId));
        var result = await controller.GetMe(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(response, okResult.Value);
    }

    [Fact]
    public async Task UpdateMe_WhenAuthorized_ReturnsOkWithUpdatedResponse()
    {
        var userId = Guid.NewGuid();
        var request = new UpdateUserRequest { Username = "new-name", Email = "new@test.com" };
        var response = SampleResponse(userId);
        _userService.Setup(x => x.UpdateAsync(userId, request, It.IsAny<CancellationToken>())).ReturnsAsync(Result<UserResponse>.Success(response));

        var controller = CreateController(BuildPrincipalWithSub(userId));
        var result = await controller.UpdateMe(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(response, okResult.Value);
    }

    [Fact]
    public async Task UpdateMe_WhenUnauthorized_ReturnsUnauthorized()
    {
        var request = new UpdateUserRequest { Username = "new-name", Email = "new@test.com" };
        var controller = CreateController();

        var result = await controller.UpdateMe(request, CancellationToken.None);

        AssertUnauthorizedProblem(result.Result);
        _userService.Verify(x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateUserRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteMe_WhenAuthorized_ReturnsNoContent()
    {
        var userId = Guid.NewGuid();
        _userService.Setup(x => x.DeleteAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        var controller = CreateController(BuildPrincipalWithSub(userId));

        var result = await controller.DeleteMe(CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        _userService.Verify(x => x.DeleteAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteMe_WhenUnauthorized_ReturnsUnauthorized()
    {
        var controller = CreateController();

        var result = await controller.DeleteMe(CancellationToken.None);

        AssertUnauthorizedProblem(result);
        _userService.Verify(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
