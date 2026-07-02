using System.Net;
using LCB.Application.Helpers;
using LCB.Domain.Extensions;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Interfaces.Services;
using LCB.Domain.Objects;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LCB.Application.Commands.Recover;

public class RecoverHandler(
    IUserRepository repository,
    IRecoverAntiAbuseService antiAbuseService,
    IRecoverTokenService recoverTokenService,
    IHostEnvironment hostEnvironment,
    ILogger<RecoverHandler> logger)
{
    private const int AntiAbuseMaxAttempts = 5;
    private static readonly TimeSpan AntiAbuseWindow = TimeSpan.FromMinutes(1);

    public Task<Result<RecoverResponse>> Handle(RecoverRequest request, string remoteIpAddress)
        => OperationExecutor.ExecuteAsync(logger, nameof(RecoverHandler), () => ExecuteAsync(request, remoteIpAddress));

    private async Task<Result<RecoverResponse>> ExecuteAsync(RecoverRequest request, string remoteIpAddress)
    {
        var normalizedEmail = request.Email.NormalizeEmail();

        if (string.IsNullOrWhiteSpace(normalizedEmail))
            return Result<RecoverResponse>.Fail("Email is required", HttpStatusCode.UnprocessableEntity);

        if (!normalizedEmail.IsValidEmail())
            return Result<RecoverResponse>.Fail("Invalid email format", HttpStatusCode.UnprocessableEntity);

        if (ShouldApplyAntiAbuse())
        {
            var antiAbuseKey = $"{NormalizeClientAddress(remoteIpAddress)}:{normalizedEmail}";
            if (!antiAbuseService.TryAcquire(antiAbuseKey, AntiAbuseMaxAttempts, AntiAbuseWindow))
                return Result<RecoverResponse>.Fail("Too many recover attempts. Try again later", HttpStatusCode.TooManyRequests);
        }

        var user = await repository.GetByEmailAsync(normalizedEmail);

        logger.LogInformation(
            "Recover request processed for {Email}. UserExists={UserExists}",
            FormatEmailForLogs(normalizedEmail),
            user is not null);

        var response = new RecoverResponse(
            BuildRecoverMessage(),
            ShouldReturnTemporaryToken() ? recoverTokenService.GenerateTemporaryResetToken() : null);

        return Result<RecoverResponse>.Ok(response, HttpStatusCode.OK);
    }

    private bool ShouldApplyAntiAbuse() => !IsTestEnvironment();

    private bool ShouldReturnTemporaryToken() => hostEnvironment.IsDevelopment() || IsTestEnvironment();

    private bool ShouldLogEmailAsPlainText() => hostEnvironment.IsDevelopment() || IsTestEnvironment();

    private bool IsTestEnvironment()
    {
        var name = hostEnvironment.EnvironmentName;
        return string.Equals(name, "Test", StringComparison.OrdinalIgnoreCase)
               || string.Equals(name, "Testing", StringComparison.OrdinalIgnoreCase);
    }

    private string BuildRecoverMessage()
    {
        if (ShouldReturnTemporaryToken())
            return "If the email exists, recovery instructions were generated for development/test.";

        return "If the email exists, recovery instructions will be sent. Email recovery delivery is not implemented yet.";
    }

    private string FormatEmailForLogs(string normalizedEmail)
    {
        if (ShouldLogEmailAsPlainText())
            return normalizedEmail;

        var parts = normalizedEmail.Split('@');
        if (parts.Length != 2)
            return "***";

        var localPart = MaskPart(parts[0]);

        var domainSegments = parts[1].Split('.');
        if (domainSegments.Length == 0)
            return $"{localPart}@***";

        domainSegments[0] = MaskPart(domainSegments[0]);
        return $"{localPart}@{string.Join('.', domainSegments)}";
    }

    private static string MaskPart(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "***";

        if (value.Length == 1)
            return "*";

        if (value.Length == 2)
            return $"{value[0]}*";

        return $"{value[0]}***{value[^1]}";
    }

    private static string NormalizeClientAddress(string remoteIpAddress)
        => string.IsNullOrWhiteSpace(remoteIpAddress)
            ? "unknown"
            : remoteIpAddress.Trim().ToLowerInvariant();
}
