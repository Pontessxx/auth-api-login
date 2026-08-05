namespace auth_api_login.Tests.Application.Common;

public class JwtSettingsTests
{
    [Fact]
    public void DefaultValues_MatchExpectedDefaults()
    {
        var settings = new JwtSettings();

        Assert.Equal("Jwt", JwtSettings.SectionName);
        Assert.Equal(string.Empty, settings.Key);
        Assert.Equal("auth-api-login", settings.Issuer);
        Assert.Equal("auth-api-login-clients", settings.Audience);
        Assert.Equal(15, settings.AccessTokenExpirationMinutes);
        Assert.Equal(7, settings.RefreshTokenExpirationDays);
        Assert.Equal(5, settings.MaxActiveSessionsPerUser);
    }

    [Fact]
    public void Properties_CanBeOverridden()
    {
        var settings = new JwtSettings
        {
            Key = "custom-key",
            Issuer = "custom-issuer",
            Audience = "custom-audience",
            AccessTokenExpirationMinutes = 30,
            RefreshTokenExpirationDays = 14,
            MaxActiveSessionsPerUser = 10
        };

        Assert.Equal("custom-key", settings.Key);
        Assert.Equal("custom-issuer", settings.Issuer);
        Assert.Equal("custom-audience", settings.Audience);
        Assert.Equal(30, settings.AccessTokenExpirationMinutes);
        Assert.Equal(14, settings.RefreshTokenExpirationDays);
        Assert.Equal(10, settings.MaxActiveSessionsPerUser);
    }
}
