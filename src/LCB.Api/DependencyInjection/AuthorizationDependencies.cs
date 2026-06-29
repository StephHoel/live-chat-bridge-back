using LCB.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace LCB.Api.DependencyInjection;

public static class AuthorizationDependencies
{
    public static IServiceCollection ConfigureAuthorization(this IServiceCollection services)
    {
        var authScheme = JwtBearerDefaults.AuthenticationScheme;
        var builder = new AuthorizationPolicyBuilder();

        services
            .AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.ProtectedApi,
                       policy => policy.AddAuthenticationSchemes(authScheme).RequireAuthenticatedUser())
            .SetFallbackPolicy(builder
                .AddAuthenticationSchemes(authScheme)
                .RequireAssertion(context =>
                {
                    if (context.Resource is HttpContext httpContext)
                    {
                        var environment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
                        var isSwaggerRoute = httpContext.Request.Path.StartsWithSegments("/swagger");

                        if (environment.IsDevelopment() && isSwaggerRoute)
                            return true;
                    }

                    return context.User?.Identity?.IsAuthenticated == true;
                })
                .Build());

        return services;
    }
}