using Emby.Plugins.InternetDbCollections.Collector;
using Emby.Plugins.InternetDbCollections.Configuration;
using Emby.Plugins.InternetDbCollections.Models.Collection;
using MediaBrowser.Model.Logging;
using Moq;

namespace Emby.Plugins.InternetDbCollections.Tests;

public class CollectorBuilderTests
{
    [Fact]
    public void Build_without_cron_keeps_enabled_collectors_even_if_schedule_is_not_due()
    {
        var now = DateTime.UtcNow;
        var config = new PluginConfiguration
        {
            Collectors =
            [
                new CollectorConfiguration
                {
                    Type = CollectorType.ImdbChart,
                    Id = "top",
                    Schedule = "* * * * *",
                    LastCollected = now,
                },
                new CollectorConfiguration
                {
                    Type = CollectorType.ImdbList,
                    Id = "disabled",
                    Enabled = false,
                },
            ],
        };

        var collectors = new CollectorBuilder()
            .UseConfig(config)
            .UseLogManager(CreateLogManager())
            .UseLogPrefix("Tests")
            .Build();

        Assert.Single(collectors);
        Assert.Equal("top", collectors[0].Config.Id);
        Assert.Equal(CollectorType.ImdbChart, collectors[0].Config.Type);
    }

    [Fact]
    public void Build_with_cron_skips_collectors_that_are_not_due()
    {
        var now = DateTime.UtcNow;
        var config = new PluginConfiguration
        {
            Collectors =
            [
                new CollectorConfiguration
                {
                    Type = CollectorType.ImdbChart,
                    Id = "due",
                    Schedule = "* * * * *",
                    LastCollected = now.AddHours(-2),
                },
                new CollectorConfiguration
                {
                    Type = CollectorType.ImdbChart,
                    Id = "recent",
                    Schedule = "* * * * *",
                    LastCollected = now,
                },
            ],
        };

        var collectors = new CollectorBuilder()
            .UseConfig(config)
            .UseLogManager(CreateLogManager())
            .UseLogPrefix("Tests")
            .EnableCron()
            .Build();

        Assert.Single(collectors);
        Assert.Equal("due", collectors[0].Config.Id);
    }

    private static ILogManager CreateLogManager()
    {
        var logger = Mock.Of<ILogger>();
        var logManager = new Mock<ILogManager>();
        logManager.Setup(x => x.GetLogger(It.IsAny<string>())).Returns(logger);
        return logManager.Object;
    }
}
