namespace auth_api_login.Tests.Api.Integration;

public class FilesEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public FilesEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<AuthResponse> RegisterAsync()
    {
        var request = new RegisterRequest
        {
            Username = $"user{Guid.NewGuid():N}"[..12],
            Email = $"{Guid.NewGuid():N}@test.com",
            Password = "1234567"
        };
        var response = await _client.PostAsJsonAsync("/api/v1/auth-service/register", request);
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private static HttpRequestMessage BuildUploadRequest(string accessToken, byte[] content, string contentType, string fileName)
    {
        var fileContent = new ByteArrayContent(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var form = new MultipartFormDataContent
        {
            { fileContent, "file", fileName }
        };

        var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/files/upload-image")
        {
            Content = form
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return message;
    }

    [Fact]
    public async Task UploadImage_WithValidPng_Returns201AndUrl()
    {
        var auth = await RegisterAsync();

        using var request = BuildUploadRequest(auth.AccessToken, [1, 2, 3, 4], "image/png", "photo.png");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UploadImageResponse>();
        Assert.StartsWith("https://fake-storage.example.com/files/", body!.Url);
        Assert.EndsWith(".png", body.Url);
    }

    [Fact]
    public async Task UploadImage_WithoutToken_Returns401()
    {
        var fileContent = new ByteArrayContent([1, 2, 3, 4]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        using var form = new MultipartFormDataContent { { fileContent, "file", "photo.png" } };

        var response = await _client.PostAsync("/api/v1/files/upload-image", form);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadImage_WithDisallowedContentType_Returns400()
    {
        var auth = await RegisterAsync();

        using var request = BuildUploadRequest(auth.AccessToken, [1, 2, 3, 4], "application/pdf", "document.pdf");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadImage_ExceedingMaxSize_Returns400()
    {
        var auth = await RegisterAsync();
        var oversizedContent = new byte[5 * 1024 * 1024 + 1];

        using var request = BuildUploadRequest(auth.AccessToken, oversizedContent, "image/png", "big.png");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
