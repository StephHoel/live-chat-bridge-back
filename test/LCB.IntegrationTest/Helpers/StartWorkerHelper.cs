using System.Net;
using System.Net.Http.Json;
using LCB.Application.Commands.Worker.Get;
using LCB.Application.Commands.Worker.Start;
using Xunit;

namespace LCB.IntegrationTest.Helpers;

public static class StartWorkerHelper
{
    public static async Task<HttpResponseMessage> StartWorkerAsync(this HttpClient client, string? tiktokUsername = null, string? twitchUsername = null, string? youtubeUsername = null)
    {
        await client.ConfigureLiveAsync(tiktokUsername, twitchUsername, youtubeUsername);

        var request = new WorkerStartRequest(
            !string.IsNullOrEmpty(tiktokUsername),
            !string.IsNullOrEmpty(twitchUsername),
            !string.IsNullOrEmpty(youtubeUsername)
        );

        var response = await client.PostAsJsonAsync("/worker/start", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.ReadResultAsync<GetWorkerStatusResponse>();
        Assert.NotNull(body);

        return response;
    }
}