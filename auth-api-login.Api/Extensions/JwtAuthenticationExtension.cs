namespace auth_api_login.Api.Extensions;

public static class JwtAuthenticationExtension
{
    public static IServiceCollection AddJwtAuthenticationConfig(this IServiceCollection services)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtSettings>((options, jwtSettings) =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var jti = context.Principal?.FindFirstValue("jti");
                        if (string.IsNullOrEmpty(jti))
                        {
                            context.Fail("Token inválido.");
                            return;
                        }

                        var tokenBlacklistRepository = context.HttpContext.RequestServices
                            .GetRequiredService<ITokenBlacklistRepository>();

                        if (await tokenBlacklistRepository.IsRevokedAsync(jti, context.HttpContext.RequestAborted))
                        {
                            context.Fail("Token revogado.");
                        }
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }

    public static WebApplication ValidateJwtSettings(this WebApplication app)
    {
        var jwtSettings = app.Services.GetRequiredService<JwtSettings>();

        if (Encoding.UTF8.GetByteCount(jwtSettings.Key) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Key ausente ou curta demais (mínimo 32 bytes). Defina-a pela variável de ambiente Jwt__Key — nunca em appsettings.json.");
        }

        return app;
    }
}
