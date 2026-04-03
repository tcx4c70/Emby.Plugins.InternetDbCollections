using Emby.Plugins.InternetDbCollections.Collector;
using Emby.Plugins.InternetDbCollections.Configuration;
using Emby.Plugins.InternetDbCollections.Models.Collection;
using MediaBrowser.Controller.Entities.Movies;

namespace Emby.Plugins.InternetDbCollections.Tests;

public class CollectorWithConfigTests
{
    [Fact]
    public async Task CollectAsync_overrides_name_and_feature_flags_from_configuration()
    {
        var innerList = new CollectionItemList
        {
            Name = "Scraped name",
            Description = "Scraped description",
            EnableTags = false,
            EnableCollections = false,
            Items =
            [
                new CollectionItem
                {
                    Order = 1,
                    Type = nameof(Movie),
                }
            ],
        };

        var config = new CollectorConfiguration
        {
            Type = CollectorType.ImdbChart,
            Id = "top",
            Name = "Configured name",
            EnableTags = true,
            EnableCollections = false,
        };

        var collector = new CollectorWithConfig(new FakeCollector(innerList), config);
        var result = await collector.CollectAsync();

        Assert.Equal("Configured name", result.Name);
        Assert.Equal("Scraped description", result.Description);
        Assert.True(result.EnableTags);
        Assert.False(result.EnableCollections);
        Assert.Single(result.Items);
    }

    private sealed class FakeCollector(CollectionItemList result) : ICollector
    {
        public Task<CollectionItemList> CollectAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(result);
        }
    }
}
