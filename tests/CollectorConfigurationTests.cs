using Emby.Plugins.InternetDbCollections.Configuration;
using Emby.Plugins.InternetDbCollections.Models.Collection;

namespace Emby.Plugins.InternetDbCollections.Tests;

public class CollectorConfigurationTests
{
    [Fact]
    public void ShouldCollectNow_returns_false_when_disabled_or_no_actions_enabled()
    {
        var disabled = new CollectorConfiguration
        {
            Type = CollectorType.ImdbChart,
            Id = "top",
            Enabled = false,
            EnableTags = true,
            EnableCollections = true,
        };
        Assert.False(disabled.ShouldCollectNow(false));

        var noActions = new CollectorConfiguration
        {
            Type = CollectorType.ImdbChart,
            Id = "top",
            Enabled = true,
            EnableTags = false,
            EnableCollections = false,
        };
        Assert.False(noActions.ShouldCollectNow(false));
    }

    [Fact]
    public void ShouldCollectNow_returns_true_when_cron_is_disabled_and_collector_is_enabled()
    {
        var config = new CollectorConfiguration
        {
            Type = CollectorType.ImdbChart,
            Id = "top",
            Enabled = true,
            EnableTags = true,
            EnableCollections = false,
            Schedule = "* * * * *",
            LastCollected = DateTime.UtcNow,
        };

        Assert.True(config.ShouldCollectNow(false));
    }

    [Fact]
    public void ShouldCollectNow_respects_cron_schedule_and_last_collected()
    {
        var now = DateTime.UtcNow;
        var config = new CollectorConfiguration
        {
            Type = CollectorType.ImdbChart,
            Id = "top",
            Enabled = true,
            EnableTags = true,
            EnableCollections = true,
            Schedule = "* * * * *",
            LastCollected = now,
        };

        Assert.False(config.ShouldCollectNow(true));

        config.LastCollected = now.AddHours(-2);

        Assert.True(config.ShouldCollectNow(true));
    }
}
