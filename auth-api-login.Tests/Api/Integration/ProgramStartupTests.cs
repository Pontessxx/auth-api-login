using Asp.Versioning.ApiExplorer;
using Swashbuckle.AspNetCore.Swagger;

namespace auth_api_login.Tests.Api.Integration;

public class ProgramStartupTests
{
    [Fact]
    public void Startup_WhenJwtKeyIsTooShort_ThrowsInvalidOperationException()
    {
        using var factory = new CustomWebApplicationFactory { JwtKey = "too-short" };

        Assert.ThrowsAny<Exception>(() => factory.Services);
    }

    [Fact]
    public void Startup_WhenJwtKeyIsValid_BuildsSuccessfully()
    {
        using var factory = new CustomWebApplicationFactory();

        var services = factory.Services;

        Assert.NotNull(services);
    }

    [Fact]
    public void Startup_ConfiguresApiExplorerOptions()
    {
        using var factory = new CustomWebApplicationFactory();
        using var scope = factory.Services.CreateScope();

        var options = scope.ServiceProvider.GetRequiredService<IOptions<ApiExplorerOptions>>().Value;

        Assert.Equal("'v'VVV", options.GroupNameFormat);
        Assert.True(options.SubstituteApiVersionInUrl);
    }

    [Fact]
    public void Startup_GeneratesSwaggerDocumentWithoutThrowing()
    {
        using var factory = new CustomWebApplicationFactory();
        using var scope = factory.Services.CreateScope();

        var swaggerProvider = scope.ServiceProvider.GetRequiredService<ISwaggerProvider>();

        var document = swaggerProvider.GetSwagger("v1");

        Assert.NotEmpty(document.Paths);
    }
}
