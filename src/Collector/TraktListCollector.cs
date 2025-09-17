namespace Emby.Plugins.InternetDbCollections.Collector;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugins.InternetDbCollections.Common;
using Emby.Plugins.InternetDbCollections.Models.Trakt;
using MediaBrowser.Model.Logging;

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

        var list = JsonSerializer.Deserialize<TraktList>(listResponse);
        _logger.Info("Parsed Trakt list '{0}' name: {1}", _listId, list.Name);
        _logger.Info("Parsed Trakt list '{0}' description: {1}", _listId, list.Description);

        _logger.Debug("Fetching items for Trakt list '{0}'...", _listId);
        var itemsResponse = await _httpClient.GetStringAsync($"/lists/{_listId}/items", cancellationToken);
        _logger.Debug("Received items for Trakt list '{0}', parsing...", _listId);

        var collectionItems =
            JsonSerializer.Deserialize<List<TraktItem>>(itemsResponse)
            .Select(TraktExtensions.ToCollectionItem)
            .ToList();
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
