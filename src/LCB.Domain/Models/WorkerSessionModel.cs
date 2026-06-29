using LCB.Domain.Enums;

namespace LCB.Domain.Models;

public class WorkerSessionModel
{
    public SemaphoreSlim Gate { get; } = new(1, 1);
    public WorkerStateEnum State { get; set; } = WorkerStateEnum.Inactive;
    public bool TikTok { get; set; }
    public bool Twitch { get; set; }
    public bool YouTube { get; set; }
    public CancellationTokenSource? CancellationTokenSource { get; set; }
    public Task? TikTokTask { get; set; }
}