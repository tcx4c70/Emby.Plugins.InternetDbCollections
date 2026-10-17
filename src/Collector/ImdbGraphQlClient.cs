using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugins.InternetDbCollections.Models.Collection;
using Emby.Plugins.InternetDbCollections.Utils;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;

namespace Emby.Plugins.InternetDbCollections.Collector;

internal sealed class ImdbGraphQlClient(HttpClient? httpClient = null)
{
    private readonly HttpClient _httpClient = httpClient ?? HttpClientPool.Instance.GetClient("imdb-graphql");

    public async Task<JsonObject> QueryAsync(
        string query,
        object variables,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.graphql.imdb.com/")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { query, variables }), Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("x-imdb-client-name", "imdb-web-next");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using HttpResponseMessage response = await _httpClient.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.Headers.TryGetValues("x-amzn-waf-action", out IEnumerable<string>? actions))
        {
            throw new HttpRequestException(
                $"IMDb GraphQL was blocked by AWS WAF: {string.Join(", ", actions)}.",
                null, response.StatusCode);
        }
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new HttpRequestException(
                $"IMDb GraphQL request failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).",
                null, response.StatusCode);
        }
        if (response.Content.Headers.ContentType?.MediaType is not
            ("application/json" or "application/graphql-response+json"))
        {
            throw new InvalidOperationException("IMDb GraphQL returned a non-JSON response.");
        }

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        JsonNode? payload = await JsonNode.ParseAsync(stream, cancellationToken: cancellationToken);
        JsonArray? errors = payload?["errors"]?.AsArray();
        if (errors is { Count: > 0 })
        {
            throw new InvalidOperationException($"IMDb GraphQL returned errors: {errors.ToJsonString()}");
        }
        return payload?["data"]?.AsObject()
            ?? throw new InvalidOperationException("IMDb GraphQL response is missing data.");
    }

    public static CollectionItem ParseItem(JsonNode? title, int order)
    {
        string? id = title?["id"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidOperationException("IMDb GraphQL returned an item without an IMDb ID.");
        }
        if (order < 1)
        {
            throw new InvalidOperationException($"IMDb GraphQL returned an invalid position for '{id}'.");
        }

        string? titleType = title?["titleType"]?["id"]?.GetValue<string>();
        string type = titleType switch
        {
            "movie" => nameof(Movie),
            "tvSeries" or "tvMiniSeries" => nameof(Series),
            _ => throw new NotSupportedException($"Unknown IMDb title type '{titleType}' for '{id}'."),
        };
        return new CollectionItem
        {
            Order = order,
            Ids = { { "imdb", id } },
            Type = type,
        };
    }

    public static string? GetNextCursor(JsonObject connection, int itemCount, HashSet<string> cursors)
    {
        int total = connection["total"]?.GetValue<int>()
            ?? throw new InvalidOperationException("IMDb GraphQL response is missing the total item count.");
        bool hasNextPage = connection["pageInfo"]?["hasNextPage"]?.GetValue<bool>()
            ?? throw new InvalidOperationException("IMDb GraphQL response is missing pagination information.");
        if (total < 0 || itemCount > total)
        {
            throw new InvalidOperationException("IMDb GraphQL returned an inconsistent total item count.");
        }
        if (!hasNextPage)
        {
            if (itemCount != total)
            {
                throw new InvalidOperationException(
                    $"IMDb GraphQL returned {itemCount} of {total} items without a next page.");
            }
            return null;
        }
        if (itemCount >= total || connection["edges"]?.AsArray().Count == 0)
        {
            throw new InvalidOperationException("IMDb GraphQL pagination did not return the expected items.");
        }

        string? cursor = connection["pageInfo"]?["endCursor"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(cursor) || !cursors.Add(cursor))
        {
            throw new InvalidOperationException("IMDb GraphQL returned a missing or repeated pagination cursor.");
        }
        return cursor;
    }
}
