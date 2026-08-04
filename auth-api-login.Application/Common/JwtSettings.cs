namespace auth_api_login.Application.Common;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "auth-api-login";
    public string Audience { get; set; } = "auth-api-login-clients";
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
    public int MaxActiveSessionsPerUser { get; set; } = 5;
}
