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

public class TraktListCollector : ICollector
{
    private readonly ILogger _logger;
    private readonly string _listId;
    private readonly string _clientId;
    private readonly HttpClient _httpClient;

    public TraktListCollector(string listId, string clientId, ILogger logger)
    {
        _listId = listId;
        _clientId = clientId;
        _logger = logger;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.trakt.tv"),
        };
        _httpClient.DefaultRequestHeaders.Add("trakt-api-version", "2");
        _httpClient.DefaultRequestHeaders.Add("trakt-api-key", _clientId);
    }

    public async Task<CollectionItemList> CollectAsync(CancellationToken cancellationToken = default)
    {
        _logger.Debug("Fetching Trakt list '{0}' data...", _listId);
        var listResponse = await _httpClient.GetStringAsync($"/lists/{_listId}", cancellationToken);
        _logger.Debug("Received Trakt list '{0}' data, parsing...", _listId);

        var list = JsonSerializer.Deserialize<TraktList>(listResponse) ?? throw new Exception($"Failed to parse Trakt list '{_listId}' data.");
        _logger.Info("Parsed Trakt list '{0}' name: {1}", _listId, list.Name);
        _logger.Info("Parsed Trakt list '{0}' description: {1}", _listId, list.Description);

        _logger.Debug("Fetching items for Trakt list '{0}'...", _listId);
        var itemsResponse = await _httpClient.GetStreamAsync($"/lists/{_listId}/items", cancellationToken);
        _logger.Debug("Received items for Trakt list '{0}', parsing...", _listId);

        var items = JsonSerializer.DeserializeAsyncEnumerable<TraktItem>(itemsResponse, cancellationToken: cancellationToken);
        var collectionItems =
            await items
            .Cast<TraktItem>()
            .Select(TraktExtensions.ToCollectionItem)
            .ToListAsync(cancellationToken);
        _logger.Info("Parsed Trakt List '{0}' items: {1} items", _listId, collectionItems.Count);

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
