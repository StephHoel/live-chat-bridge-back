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
            .SetFallbackPolicy(builder.AddAuthenticationSchemes(authScheme)
                                      .RequireAuthenticatedUser()
                                      .Build());

        return services;
    }
}