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

public class MdbListCollector : ICollector
{
    private readonly ILogger _logger;
    private readonly string _listId;
    private readonly string _apikey;
    private readonly HttpClient _httpClient;

    public MdbListCollector(string listId, string apikey, ILogger logger)
    {
        _listId = listId;
        _apikey = apikey;
        _logger = logger;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.mdblist.com"),
        };
    }

    public async Task<CollectionItemList> CollectAsync(CancellationToken cancellationToken = default)
    {
        _logger.Debug("Fetching MDB list '{0}' data...", _listId);
        var listResponse = await _httpClient.GetStringAsync($"/lists/{_listId}?apikey={_apikey}", cancellationToken);
        _logger.Debug("Received MDB list '{0}' data, parsing...", _listId);
        var list = JsonSerializer.Deserialize<List<MdbList>>(listResponse).FirstOrDefault();
        if (list is null)
        {
            _logger.Warn($"List with ID '{_listId}' not found.");
            throw new ArgumentException($"List with ID '{_listId}' not found.", _listId);
        }
        _logger.Info("Parsed MDB list '{0}' name: {1}", _listId, list.Name);
        _logger.Info("Parsed MDB list '{0}' description: {1}", _listId, list.Description);

        _logger.Debug("Fetching items for MDB list '{0}'...", _listId);
        var items = ListItemsAsync(cancellationToken);
        _logger.Debug("Received items for MDB list '{0}', parsing...", _listId);

        var collectionItems =
            await items
            .Select(MdbListExtensions.ToCollectionItem)
            .ToListAsync(cancellationToken);
        _logger.Info("Parsed MDB List '{0}' items: {1} items", _listId, collectionItems.Count);

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
                // 3. Slash '/' has special meaning in Emby provider ID, which means 'or' to support multiple provider
                //    IDs for a single provider. And Emby will only format external url with the part before the first
                //    slash (first provider ID for the provider).
                // Thanksfully, MDB List can handle the escaped slash "%2f" between username and listname correctly. So
                // let escape it!
                { CollectorType.MdbList.ToProviderName(), Uri.EscapeDataString($"{list.UserName}/{list.Slug}") },
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

            var response = await _httpClient.GetAsync($"/lists/{_listId}/items?offset={offset}&limit={limit}&apikey={_apikey}", cancellationToken);
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadAsStringAsync(cancellationToken);
            var batchItems = JsonSerializer.Deserialize<MdbListListItemsResponse>(data);
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
