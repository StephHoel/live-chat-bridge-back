namespace LCB.IntegrationTest.Constants;

public static class FakeData
{
    public static string BuildUniqueEmail()
        => $"integration-{Guid.NewGuid():N}@livebridge.com";
}