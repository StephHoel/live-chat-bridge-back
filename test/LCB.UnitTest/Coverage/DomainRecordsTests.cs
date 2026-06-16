using System;
using LCB.Domain.DTO;
using LCB.Domain.Enums;
using LCB.Domain.Models.Config;
using Xunit;

namespace LCB.UnitTest.Coverage;

public class DomainRecordsTests
{
    [Fact]
    public void Records_ExposeConstructorValues()
    {
        var payload = new PayloadDTO("msg", ["x"]);
        var command = new CommandDTO(TypeResultEnum.Error, payload, "corr");
        var parsed = new ParsedCommandDTO("!x", ["a", "b"], "!x a b");
        var model = new LCB.Domain.Models.ChatMessage("user", "text", "TikTok", new DateTime(2026, 1, 1));
        var config = new LiveConfig { Tiktok = "tk", Twitch = "tw", Youtube = "yt" };

        Assert.Equal(TypeResultEnum.Error, command.Type);
        Assert.Equal("msg", command.Payload!.Message);
        Assert.Equal("corr", command.CorrelationId);

        Assert.Equal("!x", parsed.Name);
        Assert.Equal("!x a b", parsed.Raw);
        Assert.Equal(new[] { "a", "b" }, parsed.Args);

        Assert.Equal("user", model.User);
        Assert.Equal("text", model.Text);
        Assert.Equal("TikTok", model.Platform);
        Assert.Equal(new DateTime(2026, 1, 1), model.CreatedAt);

        Assert.Equal("Usernames", LiveConfig.SectionName);
        Assert.Equal("tk", config.Tiktok);
        Assert.Equal("tw", config.Twitch);
        Assert.Equal("yt", config.Youtube);
    }
}
