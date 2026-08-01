namespace auth_api_login.Api.Filters;

public class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAuthorize = context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any()
            || (context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() ?? false);

        if (!hasAuthorize)
        {
            return;
        }

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", null)] = []
            }
        ];
    }
}
