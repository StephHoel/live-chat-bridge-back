using LCB.Domain.Interfaces.Services;

namespace LCB.IntegrationTest.Fakes;

public class FakeTikTokChatProvider : ITikTokChatProvider
{
    public void Connect(string tiktokUsername, string insertedByUser, CancellationToken cancellationToken)
    {
        cancellationToken.WaitHandle.WaitOne();
    }
}