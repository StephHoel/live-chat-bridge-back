using System.Net;
using LCB.Domain.Extensions;
using LCB.Domain.Objects;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace LCB.Api.DependencyInjection;

public static class JwtDependencies
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var key = configuration["JWT_KEY"].GetBytesFromJwtKey();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";

                        var result = Result<object?>.Fail("Unauthorized", HttpStatusCode.Unauthorized);
                        await context.Response.WriteAsJsonAsync(result);
                    },
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";

                        var result = Result<object?>.Fail("Forbidden", HttpStatusCode.Forbidden);
                        await context.Response.WriteAsJsonAsync(result);
                    }
                };
            });

        return services;
    }
}