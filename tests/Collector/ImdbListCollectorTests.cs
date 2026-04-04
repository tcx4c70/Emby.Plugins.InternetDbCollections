using System.Linq;
using System.Net.Http;
using System.Text.Json;
using Emby.Plugins.InternetDbCollections.Collector;
using Emby.Plugins.InternetDbCollections.Models.Collection;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Logging;
using Moq;

namespace Emby.Plugins.InternetDbCollections.Tests;

public class ImdbListCollectorTests
{
    [Fact]
    public async Task CollectAsync_parses_list_items()
    {
        var logger = Mock.Of<ILogger>();
        var json = JsonSerializer.Serialize(new
        {
            props = new
            {
                pageProps = new
                {
                    mainColumnData = new
                    {
                        list = new
                        {
                            name = new
                            {
                                originalText = "IMDb list sample",
                            },
                            description = new
                            {
                                originalText = new
                                {
                                    plainText = "Sample IMDb list description",
                                },
                            },
                            titleListItemSearch = new
                            {
                                edges = new object[]
                                {
                                    new
                                    {
                                        listItem = new
                                        {
                                            id = "tt0133093",
                                            titleType = new
                                            {
                                                id = "movie",
                                            },
                                        },
                                    },
                                    new
                                    {
                                        listItem = new
                                        {
                                            id = "tt1567718",
                                            titleType = new
                                            {
                                                id = "tvMiniSeries",
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            },
        });

        var handler = new HttpMessageHandlerStub(request =>
        {
            return request.RequestUri!.AbsolutePath switch
            {
                "/list/ls055592025/" => HttpResponseMessageFactory.Html(BuildListHtml(json)),
                _ => throw new InvalidOperationException($"Unexpected request: {request.RequestUri}"),
            };
        });

        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://www.imdb.com"),
        };

        var collector = new ImdbListCollector("ls055592025", logger, client);

        var result = await collector.CollectAsync();
        var items = result.Items.ToList();

        Assert.Equal("IMDb list sample", result.Name);
        Assert.Equal("Sample IMDb list description", result.Description);
        Assert.Equal("ls055592025", result.Ids[CollectorType.ImdbList.ToProviderName()]);
        Assert.Equal(2, items.Count);
        Assert.Equal(1, items[0].Order);
        Assert.Equal(nameof(Movie), items[0].Type);
        Assert.Equal("tt0133093", items[0].Ids["imdb"]);
        Assert.Equal(2, items[1].Order);
        Assert.Equal(nameof(Series), items[1].Type);
        Assert.Equal("tt1567718", items[1].Ids["imdb"]);
        Assert.Equal(["/list/ls055592025/"], handler.Requests);
    }

    private static string BuildListHtml(string json)
    {
        return $@"<html><head><title>IMDb list</title><script id=""__NEXT_DATA__"" type=""application/json"">{json}</script></head><body></body></html>";
    }
}
