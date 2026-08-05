using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace auth_api_login.Tests.Api.Integration;

public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static RegisterRequest NewRegisterRequest(string? email = null) => new()
    {
        Username = $"user{Guid.NewGuid():N}"[..12],
        Email = email ?? $"{Guid.NewGuid():N}@test.com",
        Password = "1234567"
    };

    [Fact]
    public async Task Register_WithNewEmail_Returns201WithTokens()
    {
        var request = NewRegisterRequest();

        var response = await _client.PostAsJsonAsync("/api/v1/auth-service/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.NotEmpty(body!.AccessToken);
        Assert.NotEmpty(body.RefreshToken);
        Assert.Equal(request.Username, body.User.Username);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var request = NewRegisterRequest();
        await _client.PostAsJsonAsync("/api/v1/auth-service/register", request);

        var duplicate = NewRegisterRequest(request.Email);
        var response = await _client.PostAsJsonAsync("/api/v1/auth-service/register", duplicate);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidPassword_Returns400()
    {
        var request = NewRegisterRequest();
        request.Password = "not-numeric";

        var response = await _client.PostAsJsonAsync("/api/v1/auth-service/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_Returns200WithTokens()
    {
        var request = NewRegisterRequest();
        await _client.PostAsJsonAsync("/api/v1/auth-service/register", request);

        var response = await _client.PostAsJsonAsync("/api/v1/auth-service/login", new LoginRequest { Email = request.Email, Password = request.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.NotEmpty(body!.AccessToken);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var request = NewRegisterRequest();
        await _client.PostAsJsonAsync("/api/v1/auth-service/register", request);

        var response = await _client.PostAsJsonAsync("/api/v1/auth-service/login", new LoginRequest { Email = request.Email, Password = "wrong123" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth-service/login", new LoginRequest { Email = "unknown@test.com", Password = "1234567" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithValidToken_RotatesAndReturns200()
    {
        var request = NewRegisterRequest();
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth-service/register", request);
        var auth = (await registerResponse.Content.ReadFromJsonAsync<AuthResponse>())!;

        var response = await _client.PostAsJsonAsync("/api/v1/auth-service/refresh", new RefreshRequest { RefreshToken = auth.RefreshToken });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rotated = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(rotated);
        Assert.NotEqual(auth.RefreshToken, rotated!.RefreshToken);
    }

    [Fact]
    public async Task Refresh_WithReusedToken_RevokesAllSessionsAndReturns401()
    {
        var request = NewRegisterRequest();
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth-service/register", request);
        var auth = (await registerResponse.Content.ReadFromJsonAsync<AuthResponse>())!;

        var firstRefresh = await _client.PostAsJsonAsync("/api/v1/auth-service/refresh", new RefreshRequest { RefreshToken = auth.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, firstRefresh.StatusCode);

        var reuse = await _client.PostAsJsonAsync("/api/v1/auth-service/refresh", new RefreshRequest { RefreshToken = auth.RefreshToken });

        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth-service/refresh", new RefreshRequest { RefreshToken = "not-a-real-token" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Validate_WithValidAccessToken_Returns200()
    {
        var request = NewRegisterRequest();
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth-service/register", request);
        var auth = (await registerResponse.Content.ReadFromJsonAsync<AuthResponse>())!;

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth-service/validate");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var response = await _client.SendAsync(httpRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ValidateResponse>();
        Assert.Equal(auth.User.Id, body!.UserId);
    }

    [Fact]
    public async Task Validate_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/auth-service/validate");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_ThenValidate_Returns401BecauseTokenIsBlacklisted()
    {
        var request = NewRegisterRequest();
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth-service/register", request);
        var auth = (await registerResponse.Content.ReadFromJsonAsync<AuthResponse>())!;

        using var logoutRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/auth-service/logout");
        logoutRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var logoutResponse = await _client.SendAsync(logoutRequest);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        using var validateRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth-service/validate");
        validateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var validateResponse = await _client.SendAsync(validateRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, validateResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_AlsoRevokesRefreshTokens()
    {
        var request = NewRegisterRequest();
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth-service/register", request);
        var auth = (await registerResponse.Content.ReadFromJsonAsync<AuthResponse>())!;

        using var logoutRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/auth-service/logout");
        logoutRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        await _client.SendAsync(logoutRequest);

        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth-service/refresh", new RefreshRequest { RefreshToken = auth.RefreshToken });

        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task Validate_WithTokenMissingJti_Returns401()
    {
        var handler = new JwtSecurityTokenHandler();
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_factory.JwtKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "test-issuer",
            audience: "test-audience",
            claims: [new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString())],
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);
        var rawToken = handler.WriteToken(token);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth-service/validate");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", rawToken);
        var response = await _client.SendAsync(httpRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
