using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugins.InternetDbCollections.Models.Collection;
using Emby.Plugins.InternetDbCollections.Models.Trakt;
using Emby.Plugins.InternetDbCollections.Utils;
using MediaBrowser.Model.Logging;

namespace Emby.Plugins.InternetDbCollections.Collector;

public class TraktListCollector(string listId, string clientId, ILogger logger) : ICollector
{
    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("https://api.trakt.tv"),
        DefaultRequestHeaders =
        {
            { "User-Agent", "EmbyServer" },
            { "trakt-api-version", "2" },
            { "trakt-api-key", clientId },
        },
    };

    public async Task<CollectionItemList> CollectAsync(CancellationToken cancellationToken = default)
    {
        logger.Debug("Fetching Trakt list '{0}' data...", listId);
        var listResponse = await _httpClient.GetStringAsync($"/lists/{listId}", cancellationToken);
        logger.Debug("Received Trakt list '{0}' data, parsing...", listId);

        var list = JsonSerializer.Deserialize<TraktList>(listResponse) ?? throw new Exception($"Failed to parse Trakt list '{listId}' data.");
        logger.Info("Parsed Trakt list '{0}' name: {1}", listId, list.Name);
        logger.Info("Parsed Trakt list '{0}' description: {1}", listId, list.Description);

        logger.Debug("Fetching items for Trakt list '{0}'...", listId);
        var itemsResponse = await _httpClient.GetStreamAsync($"/lists/{listId}/items", cancellationToken);
        logger.Debug("Received items for Trakt list '{0}', parsing...", listId);

        var items = JsonSerializer.DeserializeAsyncEnumerable<TraktItem>(itemsResponse, cancellationToken: cancellationToken);
        var collectionItems =
            await items
            .Cast<TraktItem>()
            .Select(TraktExtensions.ToCollectionItem)
            .ToListAsync(cancellationToken);
        logger.Info("Parsed Trakt List '{0}' items: {1} items", listId, collectionItems.Count);

        return new CollectionItemList
        {
            Name = list.Name,
            Description = list.Description,
            Ids =
            {
                { CollectorType.TraktList.ToProviderName(), list.Ids.Trakt.ToString() },
            },
            Items = collectionItems,
        };
    }
}
