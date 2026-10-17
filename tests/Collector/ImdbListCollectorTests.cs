using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    public async Task CollectAsync_reads_all_pages_in_original_list_order()
    {
        var cursors = new List<string?>();
        (int Position, string Id, string Type)[] titles = Enumerable.Range(1, 205)
            .Select(index => (index, $"tt{index:0000000}", (index % 3) switch
            {
                0 => "tvMiniSeries",
                1 => "movie",
                _ => "tvSeries",
            }))
            .ToArray();
        var handler = new HttpMessageHandlerStub(request =>
        {
            JsonNode body = JsonNode.Parse(request.Content!.ReadAsStream())!;
            Assert.Contains("sort: { by: LIST_ORDER, order: ASC }", body["query"]!.GetValue<string>());
            Assert.Equal("ls055592025", body["variables"]!["id"]!.GetValue<string>());
            Assert.Equal(100, body["variables"]!["first"]!.GetValue<int>());
            string? after = body["variables"]!["after"]?.GetValue<string>();
            cursors.Add(after);
            int offset = after switch
            {
                null => 0,
                "cursor-100" => 100,
                "cursor-200" => 200,
                _ => throw new InvalidOperationException($"Unexpected cursor: {after}"),
            };
            bool hasNextPage = offset + 100 < titles.Length;
            return HttpResponseMessageFactory.Json(BuildResponse(
                titles.Skip(offset).Take(100), 205, hasNextPage, hasNextPage ? $"cursor-{offset + 100}" : null));
        });
        using var client = new HttpClient(handler);
        var collector = new ImdbListCollector("ls055592025", Mock.Of<ILogger>(), client);

        var result = await collector.CollectAsync();
        var items = result.Items.ToList();

        Assert.Equal("IMDb list sample", result.Name);
        Assert.Equal("Sample IMDb list description", result.Description);
        Assert.Equal("ls055592025", result.Ids[CollectorType.ImdbList.ToProviderName()]);
        Assert.Equal(205, items.Count);
        Assert.Equal(205, items.Select(item => item.Ids["imdb"]).Distinct().Count());
        Assert.Equal(Enumerable.Range(1, 205), items.Select(item => item.Order));
        Assert.Equal(titles.Select(title => title.Id), items.Select(item => item.Ids["imdb"]));
        Assert.All(items, item => Assert.Equal(item.Order % 3 == 1 ? nameof(Movie) : nameof(Series), item.Type));
        Assert.Equal(new string?[] { null, "cursor-100", "cursor-200" }, cursors);
        Assert.Equal(3, handler.Requests.Count);
    }

    [Fact]
    public async Task CollectAsync_preserves_absolute_positions()
    {
        var handler = new HttpMessageHandlerStub(_ => HttpResponseMessageFactory.Json(
            BuildResponse([(3, "tt0133093", "movie"), (7, "tt1567718", "tvMiniSeries")], 2)));
        using var client = new HttpClient(handler);
        var collector = new ImdbListCollector("ls055592025", Mock.Of<ILogger>(), client);

        var result = await collector.CollectAsync();

        Assert.Equal([3, 7], result.Items.Select(item => item.Order));
    }

    [Fact]
    public async Task CollectAsync_accepts_an_empty_public_list_without_a_description()
    {
        var handler = new HttpMessageHandlerStub(_ =>
            HttpResponseMessageFactory.Json(BuildResponse([], 0, description: null)));
        using var client = new HttpClient(handler);
        var collector = new ImdbListCollector("ls055592025", Mock.Of<ILogger>(), client);

        var result = await collector.CollectAsync();

        Assert.Equal("IMDb list sample", result.Name);
        Assert.Null(result.Description);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task CollectAsync_rejects_errors_on_a_later_page_instead_of_returning_partial_items()
    {
        int requests = 0;
        var handler = new HttpMessageHandlerStub(request =>
        {
            requests++;
            return requests == 1
                ? HttpResponseMessageFactory.Json(
                    BuildResponse([(1, "tt0133093", "movie")], 2, true, "next"))
                : HttpResponseMessageFactory.Json("""
                    {
                        "data": { "list": null },
                        "errors": [{ "message": "List is unavailable", "extensions": { "code": "FORBIDDEN" } }]
                    }
                    """);
        });
        using var client = new HttpClient(handler);
        var collector = new ImdbListCollector("ls055592025", Mock.Of<ILogger>(), client);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() => collector.CollectAsync());

        Assert.Contains("List is unavailable", exception.Message);
        Assert.Contains("FORBIDDEN", exception.Message);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task CollectAsync_rejects_repeated_cursors()
    {
        int requests = 0;
        var handler = new HttpMessageHandlerStub(_ =>
        {
            requests++;
            return HttpResponseMessageFactory.Json(
                BuildResponse([(requests, $"tt{requests:0000000}", "movie")], 3, true, "same-cursor"));
        });
        using var client = new HttpClient(handler);
        var collector = new ImdbListCollector("ls055592025", Mock.Of<ILogger>(), client);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() => collector.CollectAsync());

        Assert.Contains("repeated pagination cursor", exception.Message);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task CollectAsync_rejects_an_unavailable_list()
    {
        var handler = new HttpMessageHandlerStub(_ =>
            HttpResponseMessageFactory.Json("""{ "data": { "list": null } }"""));
        using var client = new HttpClient(handler);
        var collector = new ImdbListCollector("ls055592025", Mock.Of<ILogger>(), client);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() => collector.CollectAsync());

        Assert.Contains("ls055592025", exception.Message);
        Assert.Contains("not public", exception.Message);
    }

    [Fact]
    public async Task CollectAsync_rejects_items_without_positions()
    {
        JsonNode response = JsonNode.Parse(BuildResponse([(1, "tt0133093", "movie")], 1))!;
        response["data"]!["list"]!["titleListItemSearch"]!["edges"]![0]!["node"]!.AsObject()
            .Remove("absolutePosition");
        var handler = new HttpMessageHandlerStub(_ => HttpResponseMessageFactory.Json(response.ToJsonString()));
        using var client = new HttpClient(handler);
        var collector = new ImdbListCollector("ls055592025", Mock.Of<ILogger>(), client);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() => collector.CollectAsync());

        Assert.Contains("item position", exception.Message);
    }

    private static string BuildResponse(
        IEnumerable<(int Position, string Id, string Type)> titles,
        int total,
        bool hasNextPage = false,
        string? endCursor = null,
        string? description = "Sample IMDb list description")
    {
        return JsonSerializer.Serialize(new
        {
            data = new
            {
                list = new
                {
                    name = new { originalText = "IMDb list sample" },
                    description = description is null ? null : new { originalText = new { plainText = description } },
                    titleListItemSearch = new
                    {
                        total,
                        edges = titles.Select(title => new
                        {
                            node = new { absolutePosition = title.Position },
                            title = new { id = title.Id, titleType = new { id = title.Type } },
                        }),
                        pageInfo = new { hasNextPage, endCursor },
                    },
                },
            },
        });
    }
}
