using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugins.InternetDbCollections.Models.Collection;
using MediaBrowser.Model.Logging;

namespace Emby.Plugins.InternetDbCollections.Collector;

class ImdbListCollector(string listId, ILogger logger, HttpClient? httpClient = null) : ICollector
{
    private const string Query = """
        query InternetDbCollectionsList($id: ID!, $first: Int!, $after: String) {
            list(id: $id) {
                name { originalText }
                description { originalText { plainText } }
                titleListItemSearch(
                    first: $first,
                    after: $after,
                    sort: { by: LIST_ORDER, order: ASC }
                ) {
                    total
                    edges {
                        node { absolutePosition }
                        title {
                            id
                            titleType { id }
                        }
                    }
                    pageInfo { hasNextPage endCursor }
                }
            }
        }
        """;

    private readonly ImdbGraphQlClient _client = new(httpClient);

    public async Task<CollectionItemList> CollectAsync(CancellationToken cancellationToken = default)
    {
        logger.Debug("Fetching IMDb list '{0}' via GraphQL", listId);
        var collectionItems = new List<CollectionItem>();
        var cursors = new HashSet<string>(StringComparer.Ordinal);
        string? cursor = null;
        string? name = null;
        string? description = null;
        do
        {
            JsonObject data = await _client.QueryAsync(
                Query, new { id = listId, first = 100, after = cursor }, cancellationToken);
            JsonObject list = data["list"]?.AsObject()
                ?? throw new InvalidOperationException($"IMDb list '{listId}' was not found or is not public.");
            if (name is null)
            {
                name = list["name"]?["originalText"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new InvalidOperationException($"Failed to parse IMDb list '{listId}' name.");
                }
                description = list["description"]?["originalText"]?["plainText"]?.GetValue<string>();
            }

            JsonObject connection = list["titleListItemSearch"]?.AsObject()
                ?? throw new InvalidOperationException($"Failed to parse IMDb list '{listId}' items.");
            JsonArray edges = connection["edges"]?.AsArray()
                ?? throw new InvalidOperationException($"Failed to parse IMDb list '{listId}' items.");
            foreach (JsonNode? edge in edges)
            {
                int position = edge?["node"]?["absolutePosition"]?.GetValue<int>()
                    ?? throw new InvalidOperationException($"Failed to parse IMDb list '{listId}' item position.");
                collectionItems.Add(ImdbGraphQlClient.ParseItem(edge?["title"], position));
            }
            cursor = ImdbGraphQlClient.GetNextCursor(connection, collectionItems.Count, cursors);
        }
        while (cursor != null);

        logger.Info("Parsed IMDb list name: {0}", name);
        logger.Info("Parsed IMDb list '{0}' items: {1} items", listId, collectionItems.Count);
        return new CollectionItemList
        {
            Name = name ?? throw new InvalidOperationException($"Failed to parse IMDb list '{listId}' name."),
            Description = description,
            Ids =
            {
                { CollectorType.ImdbList.ToProviderName(), listId },
            },
            Items = collectionItems,
        };
    }
}
