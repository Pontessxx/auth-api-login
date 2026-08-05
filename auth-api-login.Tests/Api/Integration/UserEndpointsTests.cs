using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace auth_api_login.Tests.Api.Integration;

public class UserEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UserEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<AuthResponse> RegisterAsync(string? email = null)
    {
        var request = new RegisterRequest
        {
            Username = $"user{Guid.NewGuid():N}"[..12],
            Email = email ?? $"{Guid.NewGuid():N}@test.com",
            Password = "1234567"
        };
        var response = await _client.PostAsJsonAsync("/api/v1/auth-service/register", request);
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private HttpRequestMessage AuthorizedRequest(HttpMethod method, string url, string accessToken)
    {
        var message = new HttpRequestMessage(method, url);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return message;
    }

    [Fact]
    public async Task GetMe_WithValidToken_ReturnsUserData()
    {
        var auth = await RegisterAsync();

        using var request = AuthorizedRequest(HttpMethod.Get, "/api/v1/user/me", auth.AccessToken);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(auth.User.Id, body!.Id);
    }

    [Fact]
    public async Task GetMe_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/user/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMe_WithValidData_UpdatesUsernameAndEmail()
    {
        var auth = await RegisterAsync();
        var newEmail = $"{Guid.NewGuid():N}@test.com";

        using var request = AuthorizedRequest(HttpMethod.Put, "/api/v1/user/edit", auth.AccessToken);
        request.Content = JsonContent.Create(new UpdateUserRequest { Username = "updated-name", Email = newEmail });
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal("updated-name", body!.Username);
        Assert.Equal(newEmail, body.Email);
    }

    [Fact]
    public async Task UpdateMe_WithEmailTakenByAnotherUser_Returns409()
    {
        var first = await RegisterAsync();
        var second = await RegisterAsync();

        using var request = AuthorizedRequest(HttpMethod.Put, "/api/v1/user/edit", second.AccessToken);
        request.Content = JsonContent.Create(new UpdateUserRequest { Username = second.User.Username, Email = first.User.Email });
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMe_WithoutToken_Returns401()
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/user/edit")
        {
            Content = JsonContent.Create(new UpdateUserRequest { Username = "x", Email = "x@test.com" })
        };
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMe_WithValidToken_DeletesAccount()
    {
        var auth = await RegisterAsync();

        using var deleteRequest = AuthorizedRequest(HttpMethod.Delete, "/api/v1/user/remove", auth.AccessToken);
        var deleteResponse = await _client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var getRequest = AuthorizedRequest(HttpMethod.Get, "/api/v1/user/me", auth.AccessToken);
        var getResponse = await _client.SendAsync(getRequest);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteMe_WithoutToken_Returns401()
    {
        var response = await _client.DeleteAsync("/api/v1/user/remove");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
