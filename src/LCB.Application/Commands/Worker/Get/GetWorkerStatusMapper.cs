using LCB.Domain.Models;

namespace LCB.Application.Commands.Worker.Get;

public static class GetWorkerStatusMapper
{
    public static GetWorkerStatusResponse ToResponse(this WorkerSessionModel session)
        => new()
        {
            State = session.State,
            TikTok = session.TikTok,
            Twitch = session.Twitch,
            YouTube = session.YouTube
        };
}