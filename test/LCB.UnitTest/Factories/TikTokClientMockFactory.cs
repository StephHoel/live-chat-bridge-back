using System.Runtime.CompilerServices;
using TikTokLiveSharp.Client;

namespace LCB.UnitTest.Factories;

internal static class TikTokClientMockFactory
{
    // Mock leve sem rede: instancia o client sem executar construtor real.
    internal static TikTokLiveClient CreateMockClient()
        => (TikTokLiveClient)RuntimeHelpers.GetUninitializedObject(typeof(TikTokLiveClient));
}
