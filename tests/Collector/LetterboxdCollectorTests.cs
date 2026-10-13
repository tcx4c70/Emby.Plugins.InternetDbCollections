using System.Linq;
using System.Net;
using System.Net.Http;
using Emby.Plugins.InternetDbCollections.Collector;
using Emby.Plugins.InternetDbCollections.Models.Collection;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Logging;
using Moq;

namespace Emby.Plugins.InternetDbCollections.Tests;

public class LetterboxdCollectorTests
{
    [Fact]
    public async Task CollectAsync_parses_numbered_multi_page_lists()
    {
        var logger = Mock.Of<ILogger>();

        var handler = new HttpMessageHandlerStub(request =>
        {
            return request.RequestUri!.AbsolutePath switch
            {
                "/fcbarcelona/list/movies-everyone-should-watch-at-least-once/page/1/" => HttpResponseMessageFactory.Html(BuildPage1Html()),
                "/fcbarcelona/list/movies-everyone-should-watch-at-least-once/page/2/" => HttpResponseMessageFactory.Html(BuildPage2Html()),
                "/film/film-one/" => HttpResponseMessageFactory.Html(BuildDetailHtml("tt1000001")),
                "/film/film-two/" => HttpResponseMessageFactory.Html(BuildDetailHtml("tt1000002")),
                "/film/film-three/" => HttpResponseMessageFactory.Html(BuildDetailHtml("tt1000003")),
                _ => throw new InvalidOperationException($"Unexpected request: {request.RequestUri}"),
            };
        });

        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://letterboxd.com"),
        };

        var collector = new LetterboxdCollector("fcbarcelona/list/movies-everyone-should-watch-at-least-once", logger, client);

        var result = await collector.CollectAsync();
        var items = result.Items.ToList();

        Assert.Equal("Movies everyone should watch at least once", result.Name);
        Assert.Equal("Two paragraph note line one\nTwo paragraph note line two", result.Description);
        Assert.Equal("fcbarcelona\\list\\movies-everyone-should-watch-at-least-once", result.Ids[CollectorType.Letterboxd.ToProviderName()]);
        Assert.Equal(3, items.Count);
        Assert.Equal(1, items[0].Order);
        Assert.Equal(nameof(Movie), items[0].Type);
        Assert.Single(items[0].Ids);
        Assert.Equal("tt1000001", items[0].Ids["imdb"]);
        Assert.Equal(2, items[1].Order);
        Assert.Single(items[1].Ids);
        Assert.Equal("tt1000002", items[1].Ids["imdb"]);
        Assert.Equal(3, items[2].Order);
        Assert.Single(items[2].Ids);
        Assert.Equal("tt1000003", items[2].Ids["imdb"]);
        Assert.Contains("/fcbarcelona/list/movies-everyone-should-watch-at-least-once/page/1/", handler.Requests);
        Assert.Contains("/fcbarcelona/list/movies-everyone-should-watch-at-least-once/page/2/", handler.Requests);
        Assert.Contains("/film/film-one/", handler.Requests);
        Assert.Contains("/film/film-two/", handler.Requests);
        Assert.Contains("/film/film-three/", handler.Requests);
        Assert.Equal(5, handler.Requests.Count);
    }

    [Fact]
    public async Task CollectAsync_parses_unumbered_lists_with_short_description()
    {
        var logger = Mock.Of<ILogger>();

        var handler = new HttpMessageHandlerStub(request =>
        {
            return request.RequestUri!.AbsolutePath switch
            {
                "/simple/list/page/1/" => HttpResponseMessageFactory.Html(BuildShortPageHtml()),
                "/film/simple-one/" => HttpResponseMessageFactory.Html(BuildDetailHtml("tt2000001")),
                "/film/simple-two/" => HttpResponseMessageFactory.Html(BuildDetailHtml("tt2000002")),
                _ => throw new InvalidOperationException($"Unexpected request: {request.RequestUri}"),
            };
        });

        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://letterboxd.com"),
        };

        var collector = new LetterboxdCollector("simple/list", logger, client);

        var result = await collector.CollectAsync();
        var items = result.Items.ToList();

        Assert.Equal("Simple List", result.Name);
        Assert.Equal("Short note", result.Description);
        Assert.Equal("simple\\list", result.Ids[CollectorType.Letterboxd.ToProviderName()]);
        Assert.Equal(2, items.Count);
        Assert.Equal(1, items[0].Order);
        Assert.Single(items[0].Ids);
        Assert.Equal("tt2000001", items[0].Ids["imdb"]);
        Assert.Equal(2, items[1].Order);
        Assert.Single(items[1].Ids);
        Assert.Equal("tt2000002", items[1].Ids["imdb"]);
        Assert.Equal(["/simple/list/page/1/", "/film/simple-one/", "/film/simple-two/"], handler.Requests);
    }

    [Theory]
    [InlineData("")]
    [InlineData("<html><body>No external IDs</body></html>")]
    [InlineData("<html><body><a href=\"https://www.themoviedb.org/movie/126400/\">TMDb</a></body></html>")]
    [InlineData("www.imdb.com/title/tt1000002")]
    [InlineData("www.imdb.com/title/")]
    [InlineData("www.imdb.com/title//")]
    public async Task CollectAsync_keeps_films_without_imdb_ids(string detailHtml)
    {
        Mock<ILogger> logger = new();
        HttpMessageHandlerStub handler = new(request =>
        {
            return request.RequestUri!.AbsolutePath switch
            {
                "/fcbarcelona/list/movies-everyone-should-watch-at-least-once/page/1/" =>
                    HttpResponseMessageFactory.Html(BuildPage1Html()),
                "/fcbarcelona/list/movies-everyone-should-watch-at-least-once/page/2/" =>
                    HttpResponseMessageFactory.Html(BuildPage2Html()),
                "/film/film-one/" => HttpResponseMessageFactory.Html(BuildDetailHtml("tt1000001")),
                "/film/film-two/" => HttpResponseMessageFactory.Html(detailHtml),
                "/film/film-three/" => HttpResponseMessageFactory.Html(BuildDetailHtml("tt1000003")),
                _ => throw new InvalidOperationException($"Unexpected request: {request.RequestUri}"),
            };
        });

        using HttpClient client = new(handler)
        {
            BaseAddress = new Uri("https://letterboxd.com"),
        };

        LetterboxdCollector collector = new(
            "fcbarcelona/list/movies-everyone-should-watch-at-least-once", logger.Object, client);

        CollectionItemList result = await collector.CollectAsync();
        var items = result.Items.ToList();

        Assert.Equal(3, items.Count);
        Assert.Equal([1, 2, 3], items.Select(item => item.Order));
        Assert.All(items, item => Assert.Equal(nameof(Movie), item.Type));
        Assert.Single(items[0].Ids);
        Assert.Equal("tt1000001", items[0].Ids["imdb"]);
        Assert.Empty(items[1].Ids);
        Assert.Single(items[2].Ids);
        Assert.Equal("tt1000003", items[2].Ids["imdb"]);
        logger.Verify(log => log.Warn(
            "Could not find IMDb ID for Letterboxd film '{0}', keeping the item without provider IDs",
            It.Is<object[]>(args => args.Length == 1 && object.Equals(args[0], "film-two"))), Times.Once);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task CollectAsync_propagates_film_request_failures(HttpStatusCode statusCode)
    {
        HttpMessageHandlerStub handler = new(request =>
        {
            return request.RequestUri!.AbsolutePath switch
            {
                "/simple/list/page/1/" => HttpResponseMessageFactory.Html(BuildShortPageHtml()),
                "/film/simple-one/" => HttpResponseMessageFactory.Html(BuildDetailHtml("tt2000001")),
                "/film/simple-two/" => new HttpResponseMessage(statusCode),
                _ => throw new InvalidOperationException($"Unexpected request: {request.RequestUri}"),
            };
        });

        using HttpClient client = new(handler)
        {
            BaseAddress = new Uri("https://letterboxd.com"),
        };

        LetterboxdCollector collector = new("simple/list", Mock.Of<ILogger>(), client);

        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(() => collector.CollectAsync());

        Assert.Equal(statusCode, exception.StatusCode);
    }

    private static string BuildPage1Html()
    {
        return $@"<html><body>
<h1 class=""title-1 prettify"">Movies everyone should watch at least once</h1>
<div id=""list-notes""><p>Two paragraph note line one</p><p>Two paragraph note line two</p></div>
<ul>
  <li class=""posteritem numbered-list-item"">
    <p class=""list-number"">1</p>
    <div class=""react-component"" data-film-id=""film-one"" data-item-slug=""film-one""></div>
  </li>
  <li class=""posteritem numbered-list-item"">
    <p class=""list-number"">2</p>
    <div class=""react-component"" data-film-id=""film-two"" data-item-slug=""film-two""></div>
  </li>
</ul>
<a class=""next"" href=""/fcbarcelona/list/movies-everyone-should-watch-at-least-once/page/2/"">Next</a>
</body></html>";
    }

    private static string BuildPage2Html()
    {
        return $@"<html><body>
<ul>
  <li class=""posteritem numbered-list-item"">
    <p class=""list-number"">3</p>
    <div class=""react-component"" data-film-id=""film-three"" data-item-slug=""film-three""></div>
  </li>
</ul>
</body></html>";
    }

    private static string BuildShortPageHtml()
    {
        return $@"<html><body>
<h1 class=""title-1 prettify"">Simple List</h1>
<div class=""body-text""><p>Short note</p></div>
<ul>
  <li class=""posteritem"">
    <div class=""react-component"" data-film-id=""film-a"" data-item-slug=""simple-one""></div>
  </li>
  <li class=""posteritem"">
    <div class=""react-component"" data-film-id=""film-b"" data-item-slug=""simple-two""></div>
  </li>
</ul>
</body></html>";
    }

    private static string BuildDetailHtml(string imdbId)
    {
        return $@"<html><body>www.imdb.com/title/{imdbId}/</body></html>";
    }
}
