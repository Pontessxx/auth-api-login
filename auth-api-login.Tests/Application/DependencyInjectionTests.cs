namespace auth_api_login.Tests.Application;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_RegistersServicesWithScopedLifetime()
    {
        var services = new ServiceCollection();

        var result = services.AddApplication();

        Assert.Same(services, result);
        Assert.Contains(services, d => d.ServiceType == typeof(IAuthService)
            && d.ImplementationType == typeof(AuthService)
            && d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, d => d.ServiceType == typeof(IUserService)
            && d.ImplementationType == typeof(UserService)
            && d.Lifetime == ServiceLifetime.Scoped);
    }
}
