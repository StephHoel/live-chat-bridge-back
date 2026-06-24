using System.Net;
using LCB.Api.Extensions;
using LCB.Application.Commands.Message.Ingest;
using LCB.Domain.Objects;
using Microsoft.AspNetCore.Mvc;

namespace LCB.Api.Endpoints;

public static class MessageEndpoints
{
    public static WebApplication MapMessageEndpoints(this WebApplication app)
    {
        app.MapPost("/messages/ingest", async (MessageIngestRequest request, [FromServices] MessageIngestHandler handler) =>
        {
            var result = await handler.Handle(request);
            return result.ToMinimalResult();
        })
        .WithTags("Messages")
        .Produces((int)HttpStatusCode.OK, typeof(MessageIngestResponse))
        .Produces((int)HttpStatusCode.BadRequest, typeof(Result<MessageIngestResponse>))
        .Produces((int)HttpStatusCode.InternalServerError, typeof(Result<MessageIngestResponse>));

        return app;
    }
}
