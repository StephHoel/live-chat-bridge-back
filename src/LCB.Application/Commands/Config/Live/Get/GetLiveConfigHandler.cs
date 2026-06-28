using System.Net;
using LCB.Application.Helpers;
using LCB.Domain.Entities;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Objects;
using Microsoft.Extensions.Logging;

namespace LCB.Application.Commands.Config.Live.Get;

public class GetLiveConfigHandler(
    ILiveSettingsRepository repository,
    ILogger<GetLiveConfigHandler> logger)
{
    public Task<Result<LiveConfigResponse>> Handle(Guid userId, string userEmail)
        => OperationExecutor.ExecuteAsync(logger, nameof(GetLiveConfigHandler), () => ExecuteAsync(userId, userEmail));

    private async Task<Result<LiveConfigResponse>> ExecuteAsync(Guid userId, string userEmail)
    {
        var settings = await repository.GetByUserIdAsync(userId);

        if (settings is null)
        {
            settings = LiveSettingsEntity.Create(userId, userEmail);
            var created = await repository.UpsertAsync(settings);

            if (!created)
                return Result<LiveConfigResponse>.Fail("Could not initialize live configuration", HttpStatusCode.InternalServerError);
        }

        return Result<LiveConfigResponse>.Ok(settings.ToResponse());
    }
}
