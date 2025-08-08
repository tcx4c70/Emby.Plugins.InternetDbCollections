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

class ImdbChartCollector : ICollector
{
    private static readonly string s_imdbChartUrlTemplate = "https://www.imdb.com/chart/{0}/";
    private static readonly string s_titleBeginTag = "<title>";
    private static readonly string s_titleEndTag = "</title>";
    private static readonly string s_descriptionBeginTag = "<meta name=\"description\" content=\"";
    private static readonly string s_descriptionEndTag = "\" data-id=\"main\"/>";
    private static readonly string s_jsonDataBeginTag = "<script id=\"__NEXT_DATA__\" type=\"application/json\">";
    private static readonly string s_jsonDataEndTag = "</script>";

    private readonly ILogger _logger;
    private readonly string _chartId;

    public ImdbChartCollector(string chartId, ILogger logger)
    {
        _chartId = chartId;
        _logger = logger;
    }

    public async Task<CollectionItemList> CollectAsync(CancellationToken cancellationToken = default)
    {
        // TODO:
        // 1. retry
        // 2. proxy
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

        var url = string.Format(s_imdbChartUrlTemplate, _chartId);
        _logger.Debug("Fetching IMDb chart '{0}' data from {1}", _chartId, url);
        var response = await client.GetStringAsync(url, cancellationToken);
        _logger.Debug("Received IMDb chart '{0}' data, parsing...", _chartId);

        // Hope we can use third-party libraries to parse HTML in Emby plugin one day.
        var titleStartIdx = response.IndexOf(s_titleBeginTag) + s_titleBeginTag.Length;
        var titleEndIdx = response.IndexOf(s_titleEndTag, titleStartIdx);
        var name = response[titleStartIdx..titleEndIdx].Trim();
        _logger.Info("Parsed IMDb chart '{0}' name: {1}", _chartId, name);

        var descriptionStartIdx = response.IndexOf(s_descriptionBeginTag) + s_descriptionBeginTag.Length;
        var descriptionEndIdx = response.IndexOf(s_descriptionEndTag, descriptionStartIdx);
        var description = response[descriptionStartIdx..descriptionEndIdx].Trim();
        _logger.Info("Parsed IMDb chart '{0}' description: {1}", _chartId, description);

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
        _logger.Info("Parsed IMDb chart '{0}' items: {1} items", _chartId, collectionItems.Count);

        return new CollectionItemList
        {
            Name = name,
            Description = description,
            Items = collectionItems,
        };
    }

    private string GetImdbId(JsonNode item)
    {
        return item?["node"]?["id"]?.ToString() ??
            item?["node"]?["release"]?["titles"]?.AsArray()?.FirstOrDefault()?["id"]?.ToString() ??
            throw new NotImplementedException($"Can't parse IMDb ID for chart {_chartId}. Please open an issue on GitHub and provide the chart ID.");
    }

    private string GetItemType(JsonNode item)
    {
        var type = item?["node"]?["titleType"]?["id"]?.ToString();
        if (string.IsNullOrEmpty(type))
        {
            throw new NotImplementedException($"Can't parse item type for chart {_chartId}. Please open an issue on GitHub and provide the chart ID.");
        }
        return type switch
        {
            "movie" => nameof(Movie),
            "tvSeries" => nameof(Series),
            "tvMiniSeries" => nameof(Series),
            _ => throw new NotImplementedException($"Unknown item type '{type}' for chart {_chartId}. Please open an issue on GitHub and provide the chart ID."),
        };
    }
}
