namespace auth_api_login.Tests.Infrastructure.Security;

public class JwtTokenGeneratorTests
{
    [Fact]
    public void GenerateToken_ReturnsTokenWithExpectedClaimsAndExpiry()
    {
        var settings = new JwtSettings
        {
            Key = "a-very-long-secret-key-that-is-at-least-32-bytes",
            Issuer = "test-issuer",
            Audience = "test-audience",
            AccessTokenExpirationMinutes = 15
        };
        var generator = new JwtTokenGenerator(Options.Create(settings));
        var user = new User { Id = Guid.NewGuid(), Username = "john", Email = "john@test.com" };

        var beforeCall = DateTime.UtcNow;
        var (token, expiresAt) = generator.GenerateToken(user);
        var afterCall = DateTime.UtcNow;

        Assert.NotEmpty(token);
        Assert.InRange(expiresAt, beforeCall.AddMinutes(15), afterCall.AddMinutes(15).AddSeconds(1));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("test-issuer", jwt.Issuer);
        Assert.Equal("test-audience", jwt.Audiences.Single());
        Assert.Equal(user.Id.ToString(), jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(user.Email, jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(user.Username, jwt.Claims.Single(c => c.Type == "username").Value);
        Assert.NotEmpty(jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value);
        Assert.NotEmpty(jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Iat).Value);
    }

    [Fact]
    public void GenerateToken_ProducesUniqueJtiPerCall()
    {
        var settings = new JwtSettings { Key = "a-very-long-secret-key-that-is-at-least-32-bytes" };
        var generator = new JwtTokenGenerator(Options.Create(settings));
        var user = new User { Id = Guid.NewGuid(), Username = "john", Email = "john@test.com" };

        var (token1, _) = generator.GenerateToken(user);
        var (token2, _) = generator.GenerateToken(user);

        var jti1 = new JwtSecurityTokenHandler().ReadJwtToken(token1).Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = new JwtSecurityTokenHandler().ReadJwtToken(token2).Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        Assert.NotEqual(jti1, jti2);
    }
}
