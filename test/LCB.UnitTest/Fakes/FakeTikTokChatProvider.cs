using System.Threading;
using LCB.Domain.Interfaces.Services;

namespace LCB.UnitTest.Fakes;

public class FakeTikTokChatProvider : ITikTokChatProvider
{
    public void Connect(string tiktokUsername, string insertedByUser, CancellationToken cancellationToken)
    {
        cancellationToken.WaitHandle.WaitOne();
    }
}