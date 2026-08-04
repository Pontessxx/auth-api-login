namespace auth_api_login.Api.Extensions;

public static class OpenApiConfigurationExtension
{
    public static IServiceCollection AddOpenApiConfig(this IServiceCollection services)
    {
        var version = Environment.GetEnvironmentVariable("DOCKER_IMAGE_VERSION") ?? "dev";
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Auth API",
                Version = version,
                Description = "API da minha aplicação"
            });
            c.UseInlineDefinitionsForEnums();
            c.DocInclusionPredicate((docName, apiDesc) => apiDesc.GroupName == docName);
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Insira o token JWT"
            });
            c.OperationFilter<AuthorizeOperationFilter>();

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            }
        });
        return services;
    }

    public static IServiceCollection AddFrontendCorsConfig(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()
            ?? [];
        services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy =>
            {
                if (allowedOrigins.Length > 0)
                {
                    policy
                    .WithOrigins(allowedOrigins)
                    .AllowCredentials();
                }
                else
                {
                    policy.AllowAnyOrigin();
                }
                policy.AllowAnyHeader().AllowAnyMethod();
            });
        });
        return services;
    }

    public static IApplicationBuilder UseSwaggerUiConfig(
        this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Backend API V1");
        });
        return app;
    }
}
