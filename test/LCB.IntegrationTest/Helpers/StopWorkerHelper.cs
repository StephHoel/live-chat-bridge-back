namespace LCB.IntegrationTest.Helpers;

public static class StopWorkerHelper
{
    public static async Task<HttpResponseMessage> StopWorkerAsync(this HttpClient client)
    {
        return await client.PostAsync("/worker/stop", null);
    }
}