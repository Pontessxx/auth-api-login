var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddOpenApiConfig();
builder.Services.AddFrontendCorsConfig(builder.Configuration);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUiConfig();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthorization();

app.MapControllers();

app.Run();
