using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugins.InternetDbCollections.Models.Collection;
using Emby.Plugins.InternetDbCollections.Utils;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Logging;

namespace Emby.Plugins.InternetDbCollections.Collector;

class ImdbListCollector(string listId, ILogger logger) : ICollector
{
    private static readonly string s_jsonDataBeginTag = "<script id=\"__NEXT_DATA__\" type=\"application/json\">";
    private static readonly string s_jsonDataEndTag = "</script>";

    public async Task<CollectionItemList> CollectAsync(CancellationToken cancellationToken = default)
    {
        var client = HttpClientPool.Instance.GetClient("imdb", client => client.BaseAddress = new Uri("https://www.imdb.com"));

        logger.Debug("Fetching IMDb list '{0}' data", listId);
        var response = await client.GetStringAsync($"/list/{listId}/", cancellationToken: cancellationToken);
        logger.Debug("Received IMDb list '{0}' data, parsing...", listId);

        var dataStartIdx = response.IndexOf(s_jsonDataBeginTag) + s_jsonDataBeginTag.Length;
        var dataEndIdx = response.IndexOf(s_jsonDataEndTag, dataStartIdx);
        var jsonData = response[dataStartIdx..dataEndIdx];
        var jsonObject = JsonNode.Parse(jsonData);

        var name = jsonObject["props"]["pageProps"]["mainColumnData"]["list"]["name"]["originalText"].ToString();
        logger.Info("Parsed IMDb list name: {0}", name);

        var description = jsonObject["props"]["pageProps"]["mainColumnData"]["list"]["description"]["originalText"]["plainText"].ToString();
        logger.Info("Parsed IMDb list description: {0}", description);

        var items = jsonObject["props"]["pageProps"]["mainColumnData"]["list"]["titleListItemSearch"]["edges"].AsArray();
        var collectionItems = items
            .Select((item, idx) => new CollectionItem
            {
                Order = idx + 1,
                Ids =
                {
                    { "imdb", item["listItem"]["id"].ToString() },
                },
                Type = GetItemType(item),
            })
            .ToList();
        logger.Info("Parsed IMDb list '{0}' items: {1} items", listId, items.Count);
        return new CollectionItemList
        {
            Name = name,
            Description = description,
            Ids =
            {
                { CollectorType.ImdbList.ToProviderName(), listId },
            },
            Items = collectionItems,
        };
    }

    private string GetItemType(JsonNode item)
    {
        var type = item?["listItem"]?["titleType"]?["id"]?.ToString();
        if (string.IsNullOrEmpty(type))
        {
            throw new NotSupportedException($"Can't parse item type for chart {listId}. Please open an issue on GitHub and provide the chart ID.");
        }
        return type switch
        {
            "movie" => nameof(Movie),
            "tvSeries" => nameof(Series),
            "tvMiniSeries" => nameof(Series),
            _ => throw new NotSupportedException($"Unknown item type '{type}' for chart {listId}. Please open an issue on GitHub and provide the chart ID."),
        };
    }
}
