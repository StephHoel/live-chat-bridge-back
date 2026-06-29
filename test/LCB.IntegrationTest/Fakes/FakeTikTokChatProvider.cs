using LCB.Domain.Interfaces.Services;

namespace LCB.IntegrationTest.Fakes;

public class FakeTikTokChatProvider : ITikTokChatProvider
{
    public void Connect(string tiktokUsername, CancellationToken cancellationToken)
    {
        cancellationToken.WaitHandle.WaitOne();
    }
}