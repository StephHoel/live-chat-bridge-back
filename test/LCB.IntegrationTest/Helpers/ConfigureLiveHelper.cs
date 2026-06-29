using System.Net;
using System.Net.Http.Json;
using LCB.Application.Commands.Config.Live;
using LCB.Application.Commands.Config.Live.Put;
using Xunit;

namespace LCB.IntegrationTest.Helpers;

public static class ConfigureLiveHelper
{
    public static async Task<LiveConfigResponse> ConfigureLiveAsync(
        this HttpClient client,
        string? tikTokUsername,
        string? twitchUsername,
        string? youTubeUsername,
        long reloadTimeInSec = 5)
    {
        var request = new PutLiveConfigRequest(
            tikTokUsername,
            twitchUsername,
            youTubeUsername,
            reloadTimeInSec);

        var response = await client.PutAsJsonAsync("/config/live", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.ReadResultAsync<LiveConfigResponse>();

        Assert.True(body.Success);
        Assert.NotNull(body.Data);

        return body.Data;
    }
}