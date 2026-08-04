using System.Security.Claims;
using System.Text;
using auth_api_login.Application.Common;
using auth_api_login.Application.Interfaces;
using auth_api_login.Domain.Entities;
using auth_api_login.Domain.Interfaces;
using auth_api_login.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers(options => options.Filters.Add<DomainExceptionFilter>());

builder.Services.AddOpenApiConfig();
builder.Services.AddFrontendCorsConfig(builder.Configuration);

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException($"Seção de configuração '{JwtSettings.SectionName}' não encontrada.");

if (Encoding.UTF8.GetByteCount(jwtSettings.Key) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key ausente ou curta demais (mínimo 32 bytes). Defina-a pela variável de ambiente Jwt__Key — nunca em appsettings.json.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
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

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();

    var adminEmail = builder.Configuration["AdminSeed:Email"] ?? "admin@admin.com";
    if (!await dbContext.Users.AnyAsync(u => u.Email == adminEmail))
    {
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        dbContext.Users.Add(new User
        {
            Id = Guid.CreateVersion7(),
            Username = builder.Configuration["AdminSeed:Username"] ?? "admin",
            Email = adminEmail,
            PasswordHash = passwordHasher.Hash(builder.Configuration["AdminSeed:Password"] ?? "admin123"),
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }
}


if (app.Environment.IsDevelopment())
{
    
    app.UseSwaggerUiConfig();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();