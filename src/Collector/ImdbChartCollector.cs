using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugins.InternetDbCollections.Models.Collection;
using Emby.Plugins.InternetDbCollections.Utils;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Logging;

namespace Emby.Plugins.InternetDbCollections.Collector;

class ImdbChartCollector(string chartId, ILogger logger) : ICollector
{
    private static readonly string s_imdbChartUrlTemplate = "https://www.imdb.com/chart/{0}/";
    private static readonly string s_titleBeginTag = "<title>";
    private static readonly string s_titleEndTag = "</title>";
    private static readonly string s_descriptionBeginTag = "<meta name=\"description\" content=\"";
    private static readonly string s_descriptionEndTag = "\" data-id=\"main\"/>";
    private static readonly string s_jsonDataBeginTag = "<script id=\"__NEXT_DATA__\" type=\"application/json\">";
    private static readonly string s_jsonDataEndTag = "</script>";

    public async Task<CollectionItemList> CollectAsync(CancellationToken cancellationToken = default)
    {
        // TODO:
        // 1. retry
        // 2. proxy
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

        var url = string.Format(s_imdbChartUrlTemplate, chartId);
        logger.Debug("Fetching IMDb chart '{0}' data from {1}", chartId, url);
        var response = await client.GetStringAsync(url, 10, logger, cancellationToken: cancellationToken);
        logger.Debug("Received IMDb chart '{0}' data, parsing...", chartId);

        // Hope we can use third-party libraries to parse HTML in Emby plugin one day.
        var titleStartIdx = response.IndexOf(s_titleBeginTag) + s_titleBeginTag.Length;
        var titleEndIdx = response.IndexOf(s_titleEndTag, titleStartIdx);
        var name = response[titleStartIdx..titleEndIdx].Trim();
        logger.Info("Parsed IMDb chart '{0}' name: {1}", chartId, name);

        var descriptionStartIdx = response.IndexOf(s_descriptionBeginTag) + s_descriptionBeginTag.Length;
        var descriptionEndIdx = response.IndexOf(s_descriptionEndTag, descriptionStartIdx);
        var description = response[descriptionStartIdx..descriptionEndIdx].Trim();
        logger.Info("Parsed IMDb chart '{0}' description: {1}", chartId, description);

        var dataStartIdx = response.IndexOf(s_jsonDataBeginTag) + s_jsonDataBeginTag.Length;
        var dataEndIdx = response.IndexOf(s_jsonDataEndTag, dataStartIdx);
        var jsonData = response[dataStartIdx..dataEndIdx];
        var jsonObject = JsonNode.Parse(jsonData);
        // TODO: better error handling
        var items = jsonObject["props"]["pageProps"]["pageData"].AsObject().First().Value["edges"].AsArray();
        var collectionItems = items
            .Select((item, idx) => new CollectionItem
            {
                Order = idx + 1,
                Ids =
                {
                    { "imdb", GetImdbId(item) },
                },
                Type = GetItemType(item),
            })
            .ToList();
        logger.Info("Parsed IMDb chart '{0}' items: {1} items", chartId, collectionItems.Count);

        return new CollectionItemList
        {
            Name = name,
            Description = description,
            Ids =
            {
                { CollectorType.ImdbChart.ToProviderName(), chartId },
            },
            Items = collectionItems,
        };
    }

    private string GetImdbId(JsonNode item)
    {
        return item?["node"]?["id"]?.ToString() ??
            item?["node"]?["release"]?["titles"]?.AsArray()?.FirstOrDefault()?["id"]?.ToString() ??
            throw new NotImplementedException($"Can't parse IMDb ID for chart {chartId}. Please open an issue on GitHub and provide the chart ID.");
    }

    private string GetItemType(JsonNode item)
    {
        var type = item?["node"]?["titleType"]?["id"]?.ToString();
        if (string.IsNullOrEmpty(type))
        {
            throw new NotImplementedException($"Can't parse item type for chart {chartId}. Please open an issue on GitHub and provide the chart ID.");
        }
        return type switch
        {
            "movie" => nameof(Movie),
            "tvSeries" => nameof(Series),
            "tvMiniSeries" => nameof(Series),
            _ => throw new NotImplementedException($"Unknown item type '{type}' for chart {chartId}. Please open an issue on GitHub and provide the chart ID."),
        };
    }
}
