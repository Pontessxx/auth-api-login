namespace auth_api_login.Tests.Infrastructure;

public class DependencyInjectionTests
{
    private static IConfiguration BuildConfiguration()
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=test;Username=test;Password=test",
            ["Jwt:Key"] = "a-very-long-secret-key-that-is-at-least-32-bytes",
            ["Jwt:Issuer"] = "test-issuer",
            ["Jwt:Audience"] = "test-audience"
        };

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public void AddInfrastructure_RegistersExpectedServiceLifetimes()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration();

        var result = services.AddInfrastructure(configuration);

        Assert.Same(services, result);
        Assert.Contains(services, d => d.ServiceType == typeof(IUserRepository) && d.ImplementationType == typeof(UserRepository) && d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, d => d.ServiceType == typeof(ITokenBlacklistRepository) && d.ImplementationType == typeof(TokenBlacklistRepository) && d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, d => d.ServiceType == typeof(IRefreshTokenRepository) && d.ImplementationType == typeof(RefreshTokenRepository) && d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, d => d.ServiceType == typeof(IPasswordHasher) && d.ImplementationType == typeof(PasswordHasher) && d.Lifetime == ServiceLifetime.Singleton);
        Assert.Contains(services, d => d.ServiceType == typeof(IJwtTokenGenerator) && d.ImplementationType == typeof(JwtTokenGenerator) && d.Lifetime == ServiceLifetime.Singleton);
        Assert.Contains(services, d => d.ServiceType == typeof(IRefreshTokenGenerator) && d.ImplementationType == typeof(RefreshTokenGenerator) && d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddInfrastructure_BindsJwtSettingsAndConfiguresDbContext()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration();
        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        var jwtSettings = provider.GetRequiredService<JwtSettings>();

        Assert.Equal("a-very-long-secret-key-that-is-at-least-32-bytes", jwtSettings.Key);
        Assert.Equal("test-issuer", jwtSettings.Issuer);
        Assert.Equal("test-audience", jwtSettings.Audience);

        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.NotNull(dbContext);
    }
}
