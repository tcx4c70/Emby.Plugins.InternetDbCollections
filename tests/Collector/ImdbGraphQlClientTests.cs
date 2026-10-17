using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using Emby.Plugins.InternetDbCollections.Collector;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;

namespace Emby.Plugins.InternetDbCollections.Tests;

public class ImdbGraphQlClientTests
{
    [Fact]
    public async Task QueryAsync_posts_query_and_variables_to_the_graphql_endpoint()
    {
        const string Query = "query Test($id: ID!) { list(id: $id) { id } }";
        const string Id = "ls0123456\"";
        var handler = new HttpMessageHandlerStub(request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://api.graphql.imdb.com/", request.RequestUri!.AbsoluteUri);
            Assert.Equal("imdb-web-next", Assert.Single(request.Headers.GetValues("x-imdb-client-name")));
            Assert.Equal("application/json", request.Content!.Headers.ContentType!.MediaType);
            Assert.Equal("application/json", Assert.Single(request.Headers.Accept).MediaType);
            JsonNode body = JsonNode.Parse(request.Content.ReadAsStream())!;
            Assert.Equal(Query, body["query"]!.GetValue<string>());
            Assert.Equal(Id, body["variables"]!["id"]!.GetValue<string>());
            return HttpResponseMessageFactory.Json("""{ "data": { "list": { "id": "ls0123456" } } }""");
        });
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://www.imdb.com"),
        };
        var client = new ImdbGraphQlClient(httpClient);

        JsonObject result = await client.QueryAsync(Query, new { id = Id });

        Assert.Equal("ls0123456", result["list"]!["id"]!.GetValue<string>());
        Assert.Equal(["/"], handler.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task QueryAsync_rejects_unexpected_http_status_codes(HttpStatusCode statusCode)
    {
        var handler = new HttpMessageHandlerStub(_ =>
        {
            HttpResponseMessage response = HttpResponseMessageFactory.Json("{}");
            response.StatusCode = statusCode;
            return response;
        });
        using var httpClient = new HttpClient(handler);
        var client = new ImdbGraphQlClient(httpClient);

        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.QueryAsync("query Test { __typename }", new { first = 1 }));

        Assert.Equal(statusCode, exception.StatusCode);
        Assert.Contains($"HTTP {(int)statusCode}", exception.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.Accepted, "challenge")]
    [InlineData(HttpStatusCode.MethodNotAllowed, "captcha")]
    [InlineData(HttpStatusCode.OK, "challenge")]
    public async Task QueryAsync_reports_aws_waf_challenges(HttpStatusCode statusCode, string action)
    {
        var handler = new HttpMessageHandlerStub(_ =>
        {
            HttpResponseMessage response = HttpResponseMessageFactory.Html(
                "<script src=\"challenge.js\"></script>", ("x-amzn-waf-action", action));
            response.StatusCode = statusCode;
            return response;
        });
        using var httpClient = new HttpClient(handler);
        var client = new ImdbGraphQlClient(httpClient);

        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.QueryAsync("query Test { __typename }", new { first = 1 }));

        Assert.Equal(statusCode, exception.StatusCode);
        Assert.Contains("AWS WAF", exception.Message);
        Assert.Contains(action, exception.Message);
    }

    [Fact]
    public async Task QueryAsync_rejects_graphql_errors_even_when_data_is_present()
    {
        var handler = new HttpMessageHandlerStub(_ => HttpResponseMessageFactory.Json("""
            {
                "data": { "list": { "id": "ls0123456" } },
                "errors": [{ "message": "Access denied", "extensions": { "code": "FORBIDDEN" } }]
            }
            """));
        using var httpClient = new HttpClient(handler);
        var client = new ImdbGraphQlClient(httpClient);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.QueryAsync("query Test { __typename }", new { first = 1 }));

        Assert.Contains("Access denied", exception.Message);
        Assert.Contains("FORBIDDEN", exception.Message);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"data\":null}")]
    [InlineData("null")]
    public async Task QueryAsync_rejects_responses_without_data(string json)
    {
        var handler = new HttpMessageHandlerStub(_ => HttpResponseMessageFactory.Json(json));
        using var httpClient = new HttpClient(handler);
        var client = new ImdbGraphQlClient(httpClient);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.QueryAsync("query Test { __typename }", new { first = 1 }));

        Assert.Contains("missing data", exception.Message);
    }

    [Fact]
    public async Task QueryAsync_rejects_html_instead_of_treating_it_as_empty_data()
    {
        var handler = new HttpMessageHandlerStub(_ =>
            HttpResponseMessageFactory.Html("<html>JavaScript required</html>"));
        using var httpClient = new HttpClient(handler);
        var client = new ImdbGraphQlClient(httpClient);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.QueryAsync("query Test { __typename }", new { first = 1 }));

        Assert.Contains("non-JSON", exception.Message);
    }

    [Fact]
    public async Task QueryAsync_propagates_invalid_json()
    {
        var handler = new HttpMessageHandlerStub(_ => HttpResponseMessageFactory.Json("{"));
        using var httpClient = new HttpClient(handler);
        var client = new ImdbGraphQlClient(httpClient);

        await Assert.ThrowsAnyAsync<JsonException>(
            () => client.QueryAsync("query Test { __typename }", new { first = 1 }));
    }

    [Fact]
    public async Task QueryAsync_honors_cancellation()
    {
        var handler = new HttpMessageHandlerStub(_ => HttpResponseMessageFactory.Json("""{ "data": {} }"""));
        using var httpClient = new HttpClient(handler);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var client = new ImdbGraphQlClient(httpClient);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.QueryAsync("query Test { __typename }", new { first = 1 }, cancellation.Token));
    }

    [Theory]
    [InlineData("movie", nameof(Movie))]
    [InlineData("tvSeries", nameof(Series))]
    [InlineData("tvMiniSeries", nameof(Series))]
    public void ParseItem_maps_supported_title_types(string titleType, string expectedType)
    {
        JsonNode title = JsonNode.Parse(
            JsonSerializer.Serialize(new { id = "tt0111161", titleType = new { id = titleType } }))!;

        var item = ImdbGraphQlClient.ParseItem(title, 7);

        Assert.Equal(expectedType, item.Type);
        Assert.Equal(7, item.Order);
        Assert.Equal("tt0111161", item.Ids["imdb"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ParseItem_rejects_missing_imdb_ids(string? id)
    {
        JsonNode title = JsonNode.Parse(
            JsonSerializer.Serialize(new { id, titleType = new { id = "movie" } }))!;

        Assert.Throws<InvalidOperationException>(() => ImdbGraphQlClient.ParseItem(title, 1));
    }

    [Fact]
    public void ParseItem_rejects_null_titles()
    {
        Assert.Throws<InvalidOperationException>(() => ImdbGraphQlClient.ParseItem(null, 1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("short")]
    public void ParseItem_rejects_missing_or_unsupported_title_types(string? titleType)
    {
        JsonNode title = JsonNode.Parse(
            JsonSerializer.Serialize(new { id = "tt0111161", titleType = new { id = titleType } }))!;

        Assert.Throws<NotSupportedException>(() => ImdbGraphQlClient.ParseItem(title, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ParseItem_rejects_invalid_positions(int position)
    {
        JsonNode title = JsonNode.Parse("""{ "id": "tt0111161", "titleType": { "id": "movie" } }""")!;

        Assert.Throws<InvalidOperationException>(() => ImdbGraphQlClient.ParseItem(title, position));
    }

    [Theory]
    [InlineData("""{ "edges": [{}], "pageInfo": { "hasNextPage": false } }""", 1)]
    [InlineData("""{ "total": 1, "edges": [{}] }""", 1)]
    [InlineData("""{ "total": -1, "edges": [], "pageInfo": { "hasNextPage": false } }""", 0)]
    [InlineData("""{ "total": 0, "edges": [{}], "pageInfo": { "hasNextPage": false } }""", 1)]
    [InlineData("""{ "total": 2, "edges": [{}], "pageInfo": { "hasNextPage": false } }""", 1)]
    [InlineData("""{ "total": 2, "edges": [], "pageInfo": { "hasNextPage": true, "endCursor": "next" } }""", 0)]
    [InlineData("""{ "total": 1, "edges": [{}], "pageInfo": { "hasNextPage": true, "endCursor": "next" } }""", 1)]
    [InlineData("""{ "total": 2, "edges": [{}], "pageInfo": { "hasNextPage": true } }""", 1)]
    [InlineData("""{ "total": 2, "edges": [{}], "pageInfo": { "hasNextPage": true, "endCursor": "" } }""", 1)]
    public void GetNextCursor_rejects_incomplete_or_inconsistent_pagination(string json, int itemCount)
    {
        JsonObject connection = JsonNode.Parse(json)!.AsObject();

        Assert.Throws<InvalidOperationException>(
            () => ImdbGraphQlClient.GetNextCursor(connection, itemCount, new HashSet<string>()));
    }
}
