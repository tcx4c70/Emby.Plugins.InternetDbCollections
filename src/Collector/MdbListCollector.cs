using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugins.InternetDbCollections.Models.Collection;
using Emby.Plugins.InternetDbCollections.Models.MdbList;
using Emby.Plugins.InternetDbCollections.Utils;
using MediaBrowser.Model.Logging;

namespace Emby.Plugins.InternetDbCollections.Collector;

public class MdbListCollector(string listId, string apikey, ILogger logger) : ICollector
{
    private readonly HttpClient _httpClient = HttpClientPool.Instance.GetClient("mdblist", client => client.BaseAddress = new Uri("https://api.mdblist.com"));

    public async Task<CollectionItemList> CollectAsync(CancellationToken cancellationToken = default)
    {
        logger.Debug("Fetching MDB list '{0}' data...", listId);
        var listResponse = await _httpClient.GetStringAsync($"/lists/{listId}?apikey={apikey}", cancellationToken: cancellationToken);
        logger.Debug("Received MDB list '{0}' data, parsing...", listId);
        var list = JsonSerializer.Deserialize<List<MdbList>>(listResponse)?.FirstOrDefault();
        if (list is null)
        {
            logger.Warn($"List with ID '{listId}' not found.");
            throw new ArgumentException($"List with ID '{listId}' not found.", listId);
        }
        logger.Info("Parsed MDB list '{0}' name: {1}", listId, list.Name);
        logger.Info("Parsed MDB list '{0}' description: {1}", listId, list.Description);

        logger.Debug("Fetching items for MDB list '{0}'...", listId);
        var items = ListItemsAsync(cancellationToken);
        logger.Debug("Received items for MDB list '{0}', parsing...", listId);

        var collectionItems =
            await items
            .Select(MdbListExtensions.ToCollectionItem)
            .ToListAsync(cancellationToken);
        logger.Info("Parsed MDB List '{0}' items: {1} items", listId, collectionItems.Count);

        return new CollectionItemList
        {
            Name = list.Name,
            Description = list.Description,
            Ids =
            {
                // We need to escape _listId here because:
                // 1. _listId here actually is in format "{username}/{listname}"
                // 2. MDB List website only supports url format "https://mdblist.com/lists/{username}/{listname}" but
                //    doesn't support "https://mdblist.com/lists/{listid}"
                // 3. Forward slash '/' has special meaning in Emby provider ID, which means 'or' to support multiple
                //    provider IDs for a single provider. And Emby will only format external url with the part before
                //    the first forward slash (first provider ID for the provider).
                // Thanksfully, back slashes '\\' also work in browsers as an URL. So let use back slashes '\\'!
                { CollectorType.MdbList.ToProviderName(), $"{list.UserName}\\{list.Slug}" },
            },
            Items = collectionItems,
        };
    }

    private async IAsyncEnumerable<MdbListItem> ListItemsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        int offset = 0;
        int limit = 100;
        bool hasMore = true;

        while (hasMore)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = await _httpClient.GetAsync($"/lists/{listId}/items?offset={offset}&limit={limit}&apikey={apikey}", cancellationToken: cancellationToken);
            response.EnsureSuccessStatusCode();
            var stream = response.Content.ReadAsStream(cancellationToken);
            var batchItems = await JsonSerializer.DeserializeAsync<MdbListListItemsResponse>(stream, cancellationToken: cancellationToken) ?? throw new InvalidOperationException("Failed to deserialize response.");
            foreach (var item in batchItems.Movies.Concat(batchItems.Shows))
            {
                yield return item;
            }

            if (response.Headers.TryGetValues("x-has-more", out var hasMoreValues) && hasMoreValues.Any())
            {
                hasMore = bool.Parse(hasMoreValues.First());
            }
            else
            {
                throw new InvalidOperationException("Response does not contain 'x-has-more' header.");
            }

            offset += batchItems.Movies.Count + batchItems.Shows.Count;
        }
    }
}
