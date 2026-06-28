namespace LCB.Domain.Interfaces.Services;

public interface ITikTokChatProvider
{
    void Connect(string tiktokUsername, CancellationToken cancellationToken);
}