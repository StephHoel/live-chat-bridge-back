using System.Net;
using LCB.Api.Extensions;
using LCB.Application.Commands.Login;
using LCB.Domain.Objects;

namespace LCB.Api.Endpoints;

public static class AuthEndpoints
{
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/login", async (LoginRequest request, LoginHandler handler) =>
        {
            var result = await handler.Handle(request);

            return result.ToMinimalResult();
        })
        .WithTags("Auth")
        .Produces((int)HttpStatusCode.OK, typeof(Result<LoginResponse>))
        .Produces((int)HttpStatusCode.NotFound, typeof(Result<LoginResponse>))
        .Produces((int)HttpStatusCode.InternalServerError, typeof(Result<LoginResponse>));

        return app;
    }
}
