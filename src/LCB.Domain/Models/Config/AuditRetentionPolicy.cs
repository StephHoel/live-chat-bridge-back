namespace LCB.Domain.Models.Config;

public class AuditRetentionPolicy
{
    public const string SectionName = "AuditRetention";

    public int EndpointOperationalTtlDays { get; init; } = 30;
    public int WorkerFlowTtlDays { get; init; } = 15;
    public int SystemTaskTtlDays { get; init; } = 60;

    public int BatchSize { get; init; } = 1000;
    public int CleanupIntervalHours { get; init; } = 24;

    public int ReviewThresholdRows { get; init; } = 500_000;
    public int ReviewThresholdMb { get; init; } = 256;
}
