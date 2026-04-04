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

public class MdbListCollectorTests
{
    [Fact]
    public async Task CollectAsync_loads_items_from_injected_client()
    {
        var logger = Mock.Of<ILogger>();
        var listJson = JsonSerializer.Serialize(new[]
        {
            new
            {
                id = 7,
                name = "MDB Picks",
                description = "MDB test list",
                user_name = "tester",
                slug = "pick-list",
            },
        });
        var itemsJson = JsonSerializer.Serialize(new
        {
            movies = new[]
            {
                new
                {
                    id = 1,
                    rank = 1,
                    mediatype = "movie",
                    imdb_id = "tt1000001",
                    tvdb_id = 101,
                },
            },
            shows = new[]
            {
                new
                {
                    id = 2,
                    rank = 2,
                    mediatype = "show",
                    imdb_id = "tt1000002",
                    tvdb_id = 202,
                },
            },
        });

        var handler = new HttpMessageHandlerStub(request =>
        {
            return request.RequestUri!.AbsolutePath switch
            {
                "/lists/test-user/test-list" => HttpResponseMessageFactory.Json(listJson),
                "/lists/test-user/test-list/items" when request.RequestUri!.Query == "?offset=0&limit=100&apikey=api-key" =>
                    HttpResponseMessageFactory.Json(itemsJson, ("x-has-more", "false")),
                _ => throw new InvalidOperationException($"Unexpected request: {request.RequestUri}"),
            };
        });

        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.mdblist.com"),
        };

        var collector = new MdbListCollector("test-user/test-list", "api-key", logger, client);

        var result = await collector.CollectAsync();
        var items = result.Items.ToList();

        Assert.Equal("MDB Picks", result.Name);
        Assert.Equal("MDB test list", result.Description);
        Assert.Equal("tester\\pick-list", result.Ids[CollectorType.MdbList.ToProviderName()]);
        Assert.Equal(2, items.Count);
        Assert.Equal(1, items[0].Order);
        Assert.Equal(nameof(Movie), items[0].Type);
        Assert.Equal("tt1000001", items[0].Ids["imdb"]);
        Assert.Equal("101", items[0].Ids["tvdb"]);
        Assert.Equal(2, items[1].Order);
        Assert.Equal(nameof(Series), items[1].Type);
        Assert.Equal("tt1000002", items[1].Ids["imdb"]);
        Assert.Equal("202", items[1].Ids["tvdb"]);
        Assert.Equal(["/lists/test-user/test-list?apikey=api-key", "/lists/test-user/test-list/items?offset=0&limit=100&apikey=api-key"], handler.Requests);
    }
}
