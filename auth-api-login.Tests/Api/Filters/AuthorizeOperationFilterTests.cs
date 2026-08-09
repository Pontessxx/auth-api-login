using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;

namespace auth_api_login.Tests.Api.Filters;

public class AuthorizeOperationFilterTests
{
    [Authorize]
    private class ClassLevelAuthorizeController
    {
        public void PlainAction()
        {
        }
    }

    private static OperationFilterContext CreateContext(System.Reflection.MethodInfo methodInfo)
    {
        return new OperationFilterContext(
            new ApiDescription(),
            Mock.Of<ISchemaGenerator>(),
            new SchemaRepository(),
            new OpenApiDocument(),
            methodInfo);
    }

    [Fact]
    public void Apply_WhenMethodHasAuthorizeAttribute_SetsSecurityRequirement()
    {
        var methodInfo = typeof(AuthController).GetMethod(nameof(AuthController.Logout))!;
        var operation = new OpenApiOperation();
        var context = CreateContext(methodInfo);
        var filter = new AuthorizeOperationFilter();

        filter.Apply(operation, context);

        Assert.NotNull(operation.Security);
        Assert.Single(operation.Security);
    }

    [Fact]
    public void Apply_WhenMethodHasNoAuthorizeAttribute_DoesNotSetSecurityRequirement()
    {
        var methodInfo = typeof(AuthController).GetMethod(nameof(AuthController.Register))!;
        var operation = new OpenApiOperation();
        var context = CreateContext(methodInfo);
        var filter = new AuthorizeOperationFilter();

        filter.Apply(operation, context);

        Assert.Null(operation.Security);
    }

    [Fact]
    public void Apply_WhenDeclaringTypeHasClassLevelAuthorizeAttribute_SetsSecurityRequirement()
    {
        var methodInfo = typeof(ClassLevelAuthorizeController).GetMethod(nameof(ClassLevelAuthorizeController.PlainAction))!;
        var operation = new OpenApiOperation();
        var context = CreateContext(methodInfo);
        var filter = new AuthorizeOperationFilter();

        filter.Apply(operation, context);

        Assert.NotNull(operation.Security);
        Assert.Single(operation.Security);
    }
}
