using LCB.Domain.Enums;

namespace LCB.Application.Commands.Worker.Get;

public class GetWorkerStatusResponse
{
    public WorkerStateEnum State { get; set; }
    public bool TikTok { get; set; }
    public bool Twitch { get; set; }
    public bool YouTube { get; set; }
}