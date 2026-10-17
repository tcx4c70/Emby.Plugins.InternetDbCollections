using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using Emby.Plugins.InternetDbCollections.Collector;
using Emby.Plugins.InternetDbCollections.Configuration;
using Emby.Plugins.InternetDbCollections.Models.Collection;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Logging;
using Moq;

namespace Emby.Plugins.InternetDbCollections.Tests;

public class ImdbChartCollectorTests
{
    [Theory]
    [InlineData("top", "TOP_RATED_MOVIES", "TOP_250", "movie", nameof(Movie))]
    [InlineData("toptv", "TOP_RATED_TV_SHOWS", "TOP_250_TV", "tvSeries", nameof(Series))]
    public async Task CollectAsync_loads_complete_top_250_charts(
        string chartId, string chartType, string rankingsChartType, string titleType, string itemType)
    {
        string name = $"Fetched name for {chartId}";
        string description = $"Fetched description for {chartId}";
        (string Id, string Type)[] titles = Enumerable.Range(1, 250)
            .Select(index => (
                $"tt{index:0000000}",
                titleType == "tvSeries" && index % 2 == 0 ? "tvMiniSeries" : titleType))
            .ToArray();
        var handler = new HttpMessageHandlerStub(request =>
        {
            JsonNode body = JsonNode.Parse(request.Content!.ReadAsStream())!;
            Assert.Contains("chartTitles", body["query"]!.GetValue<string>());
            Assert.Contains("titleChartRankings", body["query"]!.GetValue<string>());
            Assert.Contains("chartName { value }", body["query"]!.GetValue<string>());
            Assert.Contains("chartDescription { value }", body["query"]!.GetValue<string>());
            Assert.Contains("@include(if: $includeMetadata)", body["query"]!.GetValue<string>());
            Assert.Equal(chartType, body["variables"]!["chart"]!["chartType"]!.GetValue<string>());
            Assert.Equal(rankingsChartType, body["variables"]!["rankingsChartType"]!.GetValue<string>());
            Assert.Equal(250, body["variables"]!["first"]!.GetValue<int>());
            Assert.True(body["variables"]!["includeMetadata"]!.GetValue<bool>());
            Assert.Null(body["variables"]!["after"]);
            return HttpResponseMessageFactory.Json(BuildResponse(titles, 250, name: name, description: description));
        });
        using var client = new HttpClient(handler);
        var collector = new ImdbChartCollector(chartId, Mock.Of<ILogger>(), client);

        var result = await collector.CollectAsync();
        var items = result.Items.ToList();

        Assert.Equal(name, result.Name);
        Assert.Equal(description, result.Description);
        Assert.Equal(chartId, result.Ids[CollectorType.ImdbChart.ToProviderName()]);
        Assert.Equal(250, items.Count);
        Assert.Equal(250, items.Select(item => item.Ids["imdb"]).Distinct().Count());
        Assert.Equal(Enumerable.Range(1, 250), items.Select(item => item.Order));
        Assert.Equal(titles.Select(title => title.Id), items.Select(item => item.Ids["imdb"]));
        Assert.All(items, item => Assert.Equal(itemType, item.Type));
        Assert.Equal(["/"], handler.Requests);
    }

    [Theory]
    [InlineData("bottom", "LOWEST_RATED_MOVIES", "BOTTOM_100")]
    [InlineData("top-english-movies", "TOP_RATED_ENGLISH_MOVIES", "TOP_250_ENGLISH")]
    [InlineData("top-rated-indian-movies", "TOP_RATED_INDIAN_MOVIES", "TOP_250_INDIA")]
    public async Task CollectAsync_maps_other_supported_chart_ids(
        string chartId, string chartType, string rankingsChartType)
    {
        var handler = new HttpMessageHandlerStub(request =>
        {
            JsonNode body = JsonNode.Parse(request.Content!.ReadAsStream())!;
            Assert.Equal(chartType, body["variables"]!["chart"]!["chartType"]!.GetValue<string>());
            Assert.Equal(rankingsChartType, body["variables"]!["rankingsChartType"]!.GetValue<string>());
            return HttpResponseMessageFactory.Json(BuildResponse([("tt0111161", "movie")], 1));
        });
        using var client = new HttpClient(handler);
        var collector = new ImdbChartCollector(chartId, Mock.Of<ILogger>(), client);

        var result = await collector.CollectAsync();

        Assert.Single(result.Items);
        Assert.Equal("Fetched chart name", result.Name);
        Assert.Equal("Fetched chart description", result.Description);
        Assert.Equal(chartId, result.Ids[CollectorType.ImdbChart.ToProviderName()]);
    }

    [Fact]
    public async Task CollectAsync_reads_remaining_chart_pages_in_order()
    {
        const string Cursor = "cursor+/=250";
        int metadataRequests = 0;
        (string Id, string Type)[] titles = Enumerable.Range(1, 251)
            .Select(index => ($"tt{index:0000000}", "movie"))
            .ToArray();
        var handler = new HttpMessageHandlerStub(request =>
        {
            JsonNode body = JsonNode.Parse(request.Content!.ReadAsStream())!;
            string? after = body["variables"]!["after"]?.GetValue<string>();
            if (after is null)
            {
                Assert.True(body["variables"]!["includeMetadata"]!.GetValue<bool>());
                metadataRequests++;
                return HttpResponseMessageFactory.Json(BuildResponse(titles.Take(250), 251, true, Cursor));
            }
            Assert.Equal(Cursor, after);
            Assert.False(body["variables"]!["includeMetadata"]!.GetValue<bool>());
            return HttpResponseMessageFactory.Json(BuildResponse(titles.Skip(250), 251, includeMetadata: false));
        });
        using var client = new HttpClient(handler);
        var collector = new ImdbChartCollector("top", Mock.Of<ILogger>(), client);

        var result = await collector.CollectAsync();
        var items = result.Items.ToList();

        Assert.Equal(251, items.Count);
        Assert.Equal(Enumerable.Range(1, 251), items.Select(item => item.Order));
        Assert.Equal(titles.Select(title => title.Id), items.Select(item => item.Ids["imdb"]));
        Assert.Equal("Fetched chart name", result.Name);
        Assert.Equal("Fetched chart description", result.Description);
        Assert.Equal(1, metadataRequests);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task CollectAsync_preserves_configured_name_and_uses_fetched_description()
    {
        var handler = new HttpMessageHandlerStub(_ =>
            HttpResponseMessageFactory.Json(BuildResponse([("tt0111161", "movie")], 1)));
        using var client = new HttpClient(handler);
        var collector = new CollectorWithConfig(
            new ImdbChartCollector("top", Mock.Of<ILogger>(), client),
            new CollectorConfiguration
            {
                Type = CollectorType.ImdbChart,
                Id = "top",
                Name = "Configured chart name",
            });

        var result = await collector.CollectAsync();

        Assert.Equal("Configured chart name", result.Name);
        Assert.Equal("Fetched chart description", result.Description);
        Assert.Equal("top", result.Ids[CollectorType.ImdbChart.ToProviderName()]);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("""{ "metadata": null }""")]
    [InlineData("""{ "metadata": {} }""")]
    [InlineData("""{ "metadata": { "chartName": { "value": null }, "chartDescription": { "value": "About" } } }""")]
    [InlineData("""{ "metadata": { "chartName": { "value": "" }, "chartDescription": { "value": "About" } } }""")]
    [InlineData("""{ "metadata": { "chartName": { "value": " " }, "chartDescription": { "value": "About" } } }""")]
    [InlineData("""{ "metadata": { "chartName": { "value": "Chart" }, "chartDescription": null } }""")]
    [InlineData("""{ "metadata": { "chartName": { "value": "Chart" }, "chartDescription": {} } }""")]
    public async Task CollectAsync_rejects_missing_metadata_instead_of_using_hardcoded_defaults(string metadata)
    {
        JsonNode response = JsonNode.Parse(BuildResponse([("tt0111161", "movie")], 1))!;
        response["data"]!["titleChartRankings"] = JsonNode.Parse(metadata);
        var handler = new HttpMessageHandlerStub(_ => HttpResponseMessageFactory.Json(response.ToJsonString()));
        using var client = new HttpClient(handler);
        var collector = new ImdbChartCollector("top", Mock.Of<ILogger>(), client);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() => collector.CollectAsync());

        Assert.Contains("IMDb chart 'top'", exception.Message);
    }

    [Fact]
    public async Task CollectAsync_rejects_incomplete_chart_results()
    {
        var handler = new HttpMessageHandlerStub(_ =>
            HttpResponseMessageFactory.Json(BuildResponse([("tt0111161", "movie")], 250)));
        using var client = new HttpClient(handler);
        var collector = new ImdbChartCollector("top", Mock.Of<ILogger>(), client);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() => collector.CollectAsync());

        Assert.Contains("1 of 250", exception.Message);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("moviemeter")]
    [InlineData("tvmeter")]
    [InlineData("top-rated-malayalam-movies")]
    [InlineData("top-rated-tamil-movies")]
    [InlineData("top-rated-telugu-movies")]
    public async Task CollectAsync_rejects_unsupported_charts_without_sending_a_request(string chartId)
    {
        var handler = new HttpMessageHandlerStub(_ => throw new InvalidOperationException("Unexpected request."));
        using var client = new HttpClient(handler);
        var collector = new ImdbChartCollector(chartId, Mock.Of<ILogger>(), client);

        NotSupportedException exception =
            await Assert.ThrowsAsync<NotSupportedException>(() => collector.CollectAsync());

        Assert.Contains(chartId, exception.Message);
        Assert.Empty(handler.Requests);
    }

    private static string BuildResponse(
        IEnumerable<(string Id, string Type)> titles,
        int total,
        bool hasNextPage = false,
        string? endCursor = null,
        string name = "Fetched chart name",
        string description = "Fetched chart description",
        bool includeMetadata = true)
    {
        var data = new JsonObject
        {
            ["chartTitles"] = JsonSerializer.SerializeToNode(new
            {
                total,
                edges = titles.Select(title => new
                {
                    node = new { id = title.Id, titleType = new { id = title.Type } },
                }),
                pageInfo = new { hasNextPage, endCursor },
            }),
        };
        if (includeMetadata)
        {
            data["titleChartRankings"] = JsonSerializer.SerializeToNode(new
            {
                metadata = new
                {
                    chartName = new { value = name },
                    chartDescription = new { value = description },
                },
            });
        }
        return new JsonObject { ["data"] = data }.ToJsonString();
    }
}
