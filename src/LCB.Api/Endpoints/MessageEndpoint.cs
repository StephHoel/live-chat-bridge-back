using System.Net;
using LCB.Api.Extensions;
using LCB.Application.Commands.Message.Ingest;
using LCB.Domain.Objects;

namespace LCB.Api.Endpoints;

public static class MessageEndpoints
{
    public static WebApplication MapMessageEndpoints(this WebApplication app)
    {
        app.MapPost("/messages/ingest", async (MessageIngestRequest request, MessageIngestHandler handler) =>
        {
            var result = await handler.Handle(request);

            return result.ToMinimalResult();
        })
        .WithTags("Messages")
        .Produces((int)HttpStatusCode.OK, typeof(Result<MessageIngestResponse>))
        .Produces((int)HttpStatusCode.NotFound, typeof(Result<MessageIngestResponse>))
        .Produces((int)HttpStatusCode.InternalServerError, typeof(Result<MessageIngestResponse>));

        return app;
    }
}
