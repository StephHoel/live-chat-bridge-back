namespace LCB.IntegrationTest.Constants;

public static class FakeData
{
    public static string BuildUniqueEmail()
        => $"integration-{Guid.NewGuid():N}@livebridge.com";

    public static string GetCorrectPass()
        => "StrongP@ss1";
    public static string GetWrongPass()
        => "WrongP@ss1";
}