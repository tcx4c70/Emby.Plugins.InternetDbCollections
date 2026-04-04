using System.Net.Http;
using System.Linq;
using System.Text.Json;
using Emby.Plugins.InternetDbCollections.Collector;
using Emby.Plugins.InternetDbCollections.Models.Collection;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Logging;
using Moq;

namespace Emby.Plugins.InternetDbCollections.Tests;

public class TraktListCollectorTests
{
    [Fact]
    public async Task CollectAsync_loads_list_and_items_from_injected_client()
    {
        var logger = Mock.Of<ILogger>();
        var listJson = JsonSerializer.Serialize(new
        {
            name = "Trakt Picks",
            description = "A small test list",
            ids = new { trakt = 42 },
        });
        var itemsJson = JsonSerializer.Serialize(new object[]
        {
            new
            {
                id = 1,
                rank = 1,
                type = "movie",
                movie = new
                {
                    title = "Movie One",
                    ids = new { imdb = "tt0000001", tmdb = 11, tvdb = 111 },
                },
            },
            new
            {
                id = 2,
                rank = 2,
                type = "show",
                show = new
                {
                    title = "Show Two",
                    ids = new { imdb = "tt0000002", tvdb = 222 },
                },
            },
        });

        var handler = new HttpMessageHandlerStub(request =>
        {
            return request.RequestUri!.AbsolutePath switch
            {
                "/lists/test-list" => HttpResponseMessageFactory.Json(listJson),
                "/lists/42/items" => HttpResponseMessageFactory.Json(itemsJson),
                _ => throw new InvalidOperationException($"Unexpected request: {request.RequestUri}"),
            };
        });

        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.trakt.tv"),
        };

        var collector = new TraktListCollector("test-list", "client-id", logger, client);

        var result = await collector.CollectAsync();
        var items = result.Items.ToList();

        Assert.Equal("Trakt Picks", result.Name);
        Assert.Equal("A small test list", result.Description);
        Assert.Equal("42", result.Ids[CollectorType.TraktList.ToProviderName()]);
        Assert.Equal(2, items.Count);
        Assert.Equal(1, items[0].Order);
        Assert.Equal(nameof(Movie), items[0].Type);
        Assert.Equal("tt0000001", items[0].Ids["imdb"]);
        Assert.Equal("11", items[0].Ids["tmdb"]);
        Assert.Equal("111", items[0].Ids["tvdb"]);
        Assert.Equal(2, items[1].Order);
        Assert.Equal(nameof(Series), items[1].Type);
        Assert.Equal("tt0000002", items[1].Ids["imdb"]);
        Assert.False(items[1].Ids.ContainsKey("tmdb"));
        Assert.Equal(["/lists/test-list", "/lists/42/items"], handler.Requests);
    }
}
