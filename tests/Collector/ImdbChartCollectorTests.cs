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

public class ImdbChartCollectorTests
{
    [Fact]
    public async Task CollectAsync_parses_top_chart_items()
    {
        var logger = Mock.Of<ILogger>();
        var json = JsonSerializer.Serialize(new
        {
            props = new
            {
                pageProps = new
                {
                    pageData = new
                    {
                        chart = new
                        {
                            edges = new object[]
                            {
                                new
                                {
                                    node = new
                                    {
                                        id = "tt0111161",
                                        titleType = new
                                        {
                                            id = "movie",
                                        },
                                    },
                                },
                                new
                                {
                                    node = new
                                    {
                                        release = new
                                        {
                                            titles = new[]
                                            {
                                                new
                                                {
                                                    id = "tt0068646",
                                                },
                                            },
                                        },
                                        titleType = new
                                        {
                                            id = "movie",
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
                "/chart/top/" => HttpResponseMessageFactory.Html(BuildChartHtml("IMDb Top 250 Movies", "Top rated movies on IMDb.", json)),
                _ => throw new InvalidOperationException($"Unexpected request: {request.RequestUri}"),
            };
        });

        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://www.imdb.com"),
        };

        var collector = new ImdbChartCollector("top", logger, client);

        var result = await collector.CollectAsync();
        var items = result.Items.ToList();

        Assert.Equal("IMDb Top 250 Movies", result.Name);
        Assert.Equal("Top rated movies on IMDb.", result.Description);
        Assert.Equal("top", result.Ids[CollectorType.ImdbChart.ToProviderName()]);
        Assert.Equal(2, items.Count);
        Assert.Equal(1, items[0].Order);
        Assert.Equal(nameof(Movie), items[0].Type);
        Assert.Equal("tt0111161", items[0].Ids["imdb"]);
        Assert.Equal(2, items[1].Order);
        Assert.Equal(nameof(Movie), items[1].Type);
        Assert.Equal("tt0068646", items[1].Ids["imdb"]);
        Assert.Equal(["/chart/top/"], handler.Requests);
    }

    [Fact]
    public async Task CollectAsync_parses_toptv_chart_items()
    {
        var logger = Mock.Of<ILogger>();
        var json = JsonSerializer.Serialize(new
        {
            props = new
            {
                pageProps = new
                {
                    pageData = new
                    {
                        tvChart = new
                        {
                            edges = new object[]
                            {
                                new
                                {
                                    node = new
                                    {
                                        id = "tt0903747",
                                        titleType = new
                                        {
                                            id = "tvSeries",
                                        },
                                    },
                                },
                                new
                                {
                                    node = new
                                    {
                                        id = "tt7366338",
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
        });

        var handler = new HttpMessageHandlerStub(request =>
        {
            return request.RequestUri!.AbsolutePath switch
            {
                "/chart/toptv/" => HttpResponseMessageFactory.Html(BuildChartHtml("IMDb Top 250 TV Shows", "Top rated TV shows on IMDb.", json)),
                _ => throw new InvalidOperationException($"Unexpected request: {request.RequestUri}"),
            };
        });

        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://www.imdb.com"),
        };

        var collector = new ImdbChartCollector("toptv", logger, client);

        var result = await collector.CollectAsync();
        var items = result.Items.ToList();

        Assert.Equal("IMDb Top 250 TV Shows", result.Name);
        Assert.Equal("Top rated TV shows on IMDb.", result.Description);
        Assert.Equal("toptv", result.Ids[CollectorType.ImdbChart.ToProviderName()]);
        Assert.Equal(2, items.Count);
        Assert.Equal(nameof(Series), items[0].Type);
        Assert.Equal("tt0903747", items[0].Ids["imdb"]);
        Assert.Equal(nameof(Series), items[1].Type);
        Assert.Equal("tt7366338", items[1].Ids["imdb"]);
        Assert.Equal(["/chart/toptv/"], handler.Requests);
    }

    private static string BuildChartHtml(string title, string description, string json)
    {
        return $@"<html><head><title>{title}</title><meta name=""description"" content=""{description}"" data-id=""main""/><script id=""__NEXT_DATA__"" type=""application/json"">{json}</script></head><body></body></html>";
    }
}
