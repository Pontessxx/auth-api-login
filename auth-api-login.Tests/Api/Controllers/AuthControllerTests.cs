using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace auth_api_login.Tests.Api.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authService = new();

    private AuthController CreateController(ClaimsPrincipal? user = null)
    {
        var controller = new AuthController(_authService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user ?? new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };
        return controller;
    }

    private static AuthResponse SampleAuthResponse() => new()
    {
        AccessToken = "access-token",
        ExpiresAt = DateTime.UtcNow.AddMinutes(15),
        RefreshToken = "refresh-token",
        RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
        User = new UserResponse { Id = Guid.NewGuid(), Username = "john", Email = "john@test.com", CreatedAt = DateTime.UtcNow }
    };

    [Fact]
    public async Task Register_ReturnsCreatedWithAuthResponse()
    {
        var request = new RegisterRequest { Username = "john", Email = "john@test.com", Password = "1234567" };
        var response = SampleAuthResponse();
        _authService.Setup(x => x.RegisterAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var controller = CreateController();
        var result = await controller.Register(request, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
        Assert.Same(response, objectResult.Value);
    }

    [Fact]
    public async Task Login_ReturnsOkWithAuthResponse()
    {
        var request = new LoginRequest { Email = "john@test.com", Password = "1234567" };
        var response = SampleAuthResponse();
        _authService.Setup(x => x.LoginAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var controller = CreateController();
        var result = await controller.Login(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(response, okResult.Value);
    }

    [Fact]
    public async Task Refresh_ReturnsOkWithAuthResponse()
    {
        var request = new RefreshRequest { RefreshToken = "raw-token" };
        var response = SampleAuthResponse();
        _authService.Setup(x => x.RefreshAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var controller = CreateController();
        var result = await controller.Refresh(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(response, okResult.Value);
    }

    private static ClaimsPrincipal BuildPrincipal(string? jti, string? exp, string? sub)
    {
        var claims = new List<Claim>();
        if (jti is not null) claims.Add(new Claim("jti", jti));
        if (exp is not null) claims.Add(new Claim("exp", exp));
        if (sub is not null) claims.Add(new Claim(JwtRegisteredClaimNames.Sub, sub));
        return new ClaimsPrincipal(new ClaimsIdentity(claims));
    }

    [Fact]
    public async Task Logout_WhenClaimsValid_CallsLogoutAndReturnsNoContent()
    {
        var userId = Guid.NewGuid();
        var expUnix = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds();
        var user = BuildPrincipal("jti-value", expUnix.ToString(), userId.ToString());

        var controller = CreateController(user);
        var result = await controller.Logout(CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        _authService.Verify(x => x.LogoutAsync(
            userId,
            "jti-value",
            DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Logout_WhenJtiMissing_ReturnsUnauthorized()
    {
        var expUnix = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds();
        var user = BuildPrincipal(null, expUnix.ToString(), Guid.NewGuid().ToString());

        var controller = CreateController(user);
        var result = await controller.Logout(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
        _authService.Verify(x => x.LogoutAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Logout_WhenExpClaimInvalid_ReturnsUnauthorized()
    {
        var user = BuildPrincipal("jti-value", "not-a-number", Guid.NewGuid().ToString());

        var controller = CreateController(user);
        var result = await controller.Logout(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Logout_WhenSubClaimInvalid_ReturnsUnauthorized()
    {
        var expUnix = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds();
        var user = BuildPrincipal("jti-value", expUnix.ToString(), "not-a-guid");

        var controller = CreateController(user);
        var result = await controller.Logout(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public void Validate_WhenClaimsValid_ReturnsOkWithResponse()
    {
        var userId = Guid.NewGuid();
        var expUnix = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds();
        var user = BuildPrincipal(null, expUnix.ToString(), userId.ToString());

        var controller = CreateController(user);
        var result = controller.Validate();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<ValidateResponse>(okResult.Value);
        Assert.Equal(userId, body.UserId);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime, body.ExpiresAt);
    }

    [Fact]
    public void Validate_WhenSubInvalid_ReturnsUnauthorized()
    {
        var expUnix = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds();
        var user = BuildPrincipal(null, expUnix.ToString(), "not-a-guid");

        var controller = CreateController(user);
        var result = controller.Validate();

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public void Validate_WhenExpInvalid_ReturnsUnauthorized()
    {
        var user = BuildPrincipal(null, "not-a-number", Guid.NewGuid().ToString());

        var controller = CreateController(user);
        var result = controller.Validate();

        Assert.IsType<UnauthorizedResult>(result);
    }
}
