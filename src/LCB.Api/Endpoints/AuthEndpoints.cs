using System.Net;
using LCB.Api.Extensions;
using LCB.Application.Commands.Login;
using LCB.Application.Commands.Register;
using LCB.Domain.Objects;
using Microsoft.AspNetCore.Mvc;

namespace LCB.Api.Endpoints;

public static class AuthEndpoints
{
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/login", async (LoginRequest request, [FromServices] LoginHandler handler) =>
        {
            var result = await handler.Handle(request);
            return result.ToMinimalResult();
        })
        .WithTags("Auth")
        .Produces((int)HttpStatusCode.OK, typeof(Result<LoginResponse>))
        .Produces((int)HttpStatusCode.Unauthorized, typeof(Result<LoginResponse>))
        .Produces((int)HttpStatusCode.InternalServerError, typeof(Result<LoginResponse>));

        app.MapPost("/auth/register", async (RegisterRequest request, [FromServices] RegisterHandler handler) =>
        {
            var result = await handler.Handle(request);
            return result.ToMinimalResult();
        })
        .WithTags("Auth")
        .Produces((int)HttpStatusCode.Created, typeof(Result<RegisterResponse>))
        .Produces((int)HttpStatusCode.BadRequest, typeof(Result<RegisterResponse>))
        .Produces((int)HttpStatusCode.Conflict, typeof(Result<RegisterResponse>))
        .Produces((int)HttpStatusCode.InternalServerError, typeof(Result<RegisterResponse>));

        return app;
    }
}
