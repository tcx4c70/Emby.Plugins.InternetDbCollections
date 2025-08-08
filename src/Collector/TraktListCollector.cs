namespace Emby.Plugins.InternetDbCollections.Collector;

using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
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

        var list = JsonNode.Parse(listResponse);
        var name = list["name"].GetValue<string>();
        var description = list["description"].GetValue<string>();
        _logger.Info("Parsed Trakt list '{0}' name: {1}", _listId, name);
        _logger.Info("Parsed Trakt list '{0}' description: {1}", _listId, description);

        _logger.Debug("Fetching items for Trakt list '{0}'...", _listId);
        var itemsResponse = await _httpClient.GetStringAsync($"/lists/{_listId}/items", cancellationToken);
        _logger.Debug("Received items for Trakt list '{0}', parsing...", _listId);

        var items = JsonNode.Parse(itemsResponse).AsArray();
        var collectionItems = items.Select(ToTraktListItem).ToList();
        _logger.Info("Parsed Trakt List '{0}' items: {1} items", _listId, collectionItems.Count);

        return new CollectionItemList
        {
            Name = name,
            Description = description,
            ProviderNames = new[] { "Imdb" },
            Items = collectionItems,
        };
    }

    private TraktListItem ToTraktListItem(JsonNode item)
    {
        var order = item["rank"].GetValue<int>();
        string id;
        string type;
        switch (item["type"].GetValue<string>())
        {
            case "movie":
                type = nameof(Movie);
                id = item["movie"]["ids"]["imdb"].GetValue<string>();
                break;
            case "show":
                type = nameof(Series);
                id = item["show"]["ids"]["imdb"].GetValue<string>();
                break;
            default:
                throw new ArgumentException($"Unknown Trakt item type: {item["type"].GetValue<string>()}");
        }

        return new TraktListItem
        {
            Order = order,
            Id = id,
            Type = type,
        };
    }

    private class TraktListItem : ICollectionItem
    {
        public int Order { get; set; }
        public string Id { get; set; }
        public string Type { get; set; }
    }
}
