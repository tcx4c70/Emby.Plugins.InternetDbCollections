using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugins.InternetDbCollections.Models.Collection;
using MediaBrowser.Model.Logging;

namespace Emby.Plugins.InternetDbCollections.Collector;

class ImdbChartCollector(string chartId, ILogger logger, HttpClient? httpClient = null) : ICollector
{
    private const string Query = """
        query InternetDbCollectionsChart(
            $chart: ChartTitleOptions!,
            $rankingsChartType: TitleChartRankingsType!,
            $first: Int!,
            $after: String,
            $includeMetadata: Boolean!
        ) {
            chartTitles(chart: $chart, first: $first, after: $after) {
                total
                edges {
                    node {
                        id
                        titleType { id }
                    }
                }
                pageInfo { hasNextPage endCursor }
            }
            titleChartRankings(first: 1, input: { rankingsChartType: $rankingsChartType })
                @include(if: $includeMetadata) {
                metadata {
                    chartName { value }
                    chartDescription { value }
                }
            }
        }
        """;

    private readonly ImdbGraphQlClient _client = new(httpClient);

    public async Task<CollectionItemList> CollectAsync(CancellationToken cancellationToken = default)
    {
        (string chartType, string rankingsChartType) = chartId switch
        {
            "top" => ("TOP_RATED_MOVIES", "TOP_250"),
            "toptv" => ("TOP_RATED_TV_SHOWS", "TOP_250_TV"),
            "bottom" => ("LOWEST_RATED_MOVIES", "BOTTOM_100"),
            "top-english-movies" => ("TOP_RATED_ENGLISH_MOVIES", "TOP_250_ENGLISH"),
            "top-rated-indian-movies" => ("TOP_RATED_INDIAN_MOVIES", "TOP_250_INDIA"),
            _ => throw new NotSupportedException($"Unsupported IMDb chart '{chartId}'."),
        };

        logger.Debug("Fetching IMDb chart '{0}' via GraphQL", chartId);
        var collectionItems = new List<CollectionItem>();
        var cursors = new HashSet<string>(StringComparer.Ordinal);
        string? cursor = null;
        string? name = null;
        string? description = null;
        do
        {
            JsonObject data = await _client.QueryAsync(
                Query,
                new
                {
                    chart = new { chartType },
                    rankingsChartType,
                    first = 250,
                    after = cursor,
                    includeMetadata = cursor is null,
                },
                cancellationToken);
            if (cursor is null)
            {
                JsonObject metadata = data["titleChartRankings"]?["metadata"]?.AsObject()
                    ?? throw new InvalidOperationException($"Failed to parse IMDb chart '{chartId}' metadata.");
                name = metadata["chartName"]?["value"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new InvalidOperationException($"Failed to parse IMDb chart '{chartId}' name.");
                }
                description = metadata["chartDescription"]?["value"]?.GetValue<string>()
                    ?? throw new InvalidOperationException($"Failed to parse IMDb chart '{chartId}' description.");
            }
            JsonObject connection = data["chartTitles"]?.AsObject()
                ?? throw new InvalidOperationException($"Failed to parse IMDb chart '{chartId}' data.");
            JsonArray edges = connection["edges"]?.AsArray()
                ?? throw new InvalidOperationException($"Failed to parse IMDb chart '{chartId}' items.");
            foreach (JsonNode? edge in edges)
            {
                collectionItems.Add(ImdbGraphQlClient.ParseItem(edge?["node"], collectionItems.Count + 1));
            }
            cursor = ImdbGraphQlClient.GetNextCursor(connection, collectionItems.Count, cursors);
        }
        while (cursor != null);

        logger.Info("Parsed IMDb chart '{0}' items: {1} items", chartId, collectionItems.Count);

        return new CollectionItemList
        {
            Name = name ?? throw new InvalidOperationException($"Failed to parse IMDb chart '{chartId}' name."),
            Description = description,
            Ids =
            {
                { CollectorType.ImdbChart.ToProviderName(), chartId },
            },
            Items = collectionItems,
        };
    }
}
