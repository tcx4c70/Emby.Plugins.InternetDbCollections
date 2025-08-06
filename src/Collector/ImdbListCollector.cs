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

class ImdbListCollector : ICollector
{
    private static readonly string s_imdbListUrlTemplate = "https://www.imdb.com/list/{0}/";
    private static readonly string s_jsonDataBeginTag = "<script id=\"__NEXT_DATA__\" type=\"application/json\">";
    private static readonly string s_jsonDataEndTag = "</script>";

    private readonly ILogger _logger;
    private readonly string _listId;


    public ImdbListCollector(string listId, ILogger logger)
    {
        _listId = listId;
        _logger = logger;
    }

    public async Task<CollectionItemList> CollectAsync(CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

        var url = string.Format(s_imdbListUrlTemplate, _listId);
        _logger.Debug("Fetching IMDb list '{0}' data from {1}", _listId, url);
        var response = await client.GetStringAsync(url, cancellationToken);
        _logger.Debug("Received IMDb list '{0}' data, parsing...", _listId);

        var dataStartIdx = response.IndexOf(s_jsonDataBeginTag) + s_jsonDataBeginTag.Length;
        var dataEndIdx = response.IndexOf(s_jsonDataEndTag, dataStartIdx);
        var jsonData = response[dataStartIdx..dataEndIdx];
        var jsonObject = JsonNode.Parse(jsonData);

        var name = jsonObject["props"]["pageProps"]["mainColumnData"]["list"]["name"]["originalText"].ToString();
        _logger.Info("Parsed IMDb list name: {0}", name);

        var description = jsonObject["props"]["pageProps"]["mainColumnData"]["list"]["description"]["originalText"]["plainText"].ToString();
        _logger.Info("Parsed IMDb list description: {0}", description);

        var items = jsonObject["props"]["pageProps"]["mainColumnData"]["list"]["titleListItemSearch"]["edges"].AsArray();
        var collectionItems = items
            .Select((item, idx) => new ImdbListItem
            {
                Order = idx + 1,
                Id = item["listItem"]["id"].ToString(),
                Type = GetItemType(item),
            })
            .ToList();
        _logger.Info("Parsed IMDb list '{0}' items: {1} items", _listId, items.Count);
        return new CollectionItemList
        {
            Name = name,
            Description = description,
            ProviderNames = new[] { "imdb" },
            Items = collectionItems,
        };
    }

    private string GetItemType(JsonNode item)
    {
        var type = item?["listItem"]?["titleType"]?["id"]?.ToString();
        if (string.IsNullOrEmpty(type))
        {
            throw new NotImplementedException($"Can't parse item type for chart {_listId}. Please open an issue on GitHub and provide the chart ID.");
        }
        return type switch
        {
            "movie" => nameof(Movie),
            "tvSeries" => nameof(Series),
            "tvMiniSeries" => nameof(Series),
            _ => throw new NotImplementedException($"Unknown item type '{type}' for chart {_listId}. Please open an issue on GitHub and provide the chart ID."),
        };
    }

    private class ImdbListItem : ICollectionItem
    {
        public int Order { get; init; }

        public string Id { get; init; }

        public string Type { get; init; }
    }
}
