using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace auth_api_login.Tests.Api.Extensions;

public class OpenApiConfigurationExtensionTests
{
    [Fact]
    public void AddOpenApiConfig_ConfiguresSwaggerGenOptions()
    {
        var services = new ServiceCollection();

        var result = services.AddOpenApiConfig();

        Assert.Same(services, result);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SwaggerGenOptions>>().Value;

        Assert.True(options.SwaggerGeneratorOptions.SwaggerDocs.ContainsKey("v1"));
        Assert.True(options.SwaggerGeneratorOptions.SecuritySchemes.ContainsKey("Bearer"));
    }

    [Fact]
    public void AddFrontendCorsConfig_WithConfiguredOrigins_RestrictsToThoseOrigins()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "https://example.com"
            })
            .Build();

        services.AddFrontendCorsConfig(configuration);

        using var provider = services.BuildServiceProvider();
        var corsOptions = provider.GetRequiredService<IOptions<CorsOptions>>().Value;
        var policy = corsOptions.GetPolicy("Frontend");

        Assert.NotNull(policy);
        Assert.Contains("https://example.com", policy!.Origins);
        Assert.False(policy.AllowAnyOrigin);
    }

    [Fact]
    public void AddFrontendCorsConfig_WithoutConfiguredOrigins_AllowsAnyOrigin()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddFrontendCorsConfig(configuration);

        using var provider = services.BuildServiceProvider();
        var corsOptions = provider.GetRequiredService<IOptions<CorsOptions>>().Value;
        var policy = corsOptions.GetPolicy("Frontend");

        Assert.NotNull(policy);
        Assert.True(policy!.AllowAnyOrigin);
    }

    [Fact]
    public void UseSwaggerUiConfig_RegistersMiddlewareWithoutThrowing()
    {
        var builder = WebApplication.CreateBuilder();
        using var app = builder.Build();

        var result = app.UseSwaggerUiConfig();

        Assert.Same(app, result);
    }
}
