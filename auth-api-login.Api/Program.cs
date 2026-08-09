using auth_api_login.Domain.Entities;
using auth_api_login.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();

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
builder.Services.AddJwtAuthenticationConfig();

var app = builder.Build();

app.ValidateJwtSettings();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();

    var adminEmail = builder.Configuration["AdminSeed:Email"]
        ?? throw new InvalidOperationException("AdminSeed:Email não configurado. Defina-a pela variável de ambiente AdminSeed__Email.");
    if (!await dbContext.Users.AnyAsync(u => u.Email == adminEmail))
    {
        var adminUsername = builder.Configuration["AdminSeed:Username"]
            ?? throw new InvalidOperationException("AdminSeed:Username não configurado. Defina-a pela variável de ambiente AdminSeed__Username.");
        var adminPassword = builder.Configuration["AdminSeed:Password"]
            ?? throw new InvalidOperationException("AdminSeed:Password não configurado. Defina-a pela variável de ambiente AdminSeed__Password.");

        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        dbContext.Users.Add(new User
        {
            Id = Guid.CreateVersion7(),
            Username = adminUsername,
            Email = adminEmail,
            PasswordHash = passwordHasher.Hash(adminPassword),
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