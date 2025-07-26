namespace Emby.Plugins.InternetDbCollections.Collector;

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;

class ImdbChartCollector : ImdbCollector
{
    private static readonly string s_imdbChartUrlTemplate = "https://www.imdb.com/chart/{0}/";
    private static readonly string s_titleBeginTag = "<title>";
    private static readonly string s_titleEndTag = "</title>";
    private static readonly string s_descriptionBeginTag = "<meta name=\"description\" content=\"";
    private static readonly string s_descriptionEndTag = "\" data-id=\"main\"/>";
    private static readonly string s_jsonDataBeginTag = "<script id=\"__NEXT_DATA__\" type=\"application/json\">";
    private static readonly string s_jsonDataEndTag = "</script>";

    public ImdbChartCollector(string chartId, ILogger logger, ILibraryManager libraryManager)
        : base (chartId, logger, libraryManager)
    {
    }

    public override string Name => $"IMDb Chart: {_name ?? _id}";

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // TODO:
        // 1. retry
        // 2. proxy
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

        var url = string.Format(s_imdbChartUrlTemplate, _id);
        _logger.Debug("Fetching IMDb chart '{0}' data from {1}", _id, url);
        var response = await client.GetStringAsync(url, cancellationToken);
        _logger.Debug("Received IMDb chart '{0}' data, parsing...", _id);

        // Hope we can use third-party libraries to parse HTML in Emby plugin one day.

        var titleStartIdx = response.IndexOf(s_titleBeginTag) + s_titleBeginTag.Length;
        var titleEndIdx = response.IndexOf(s_titleEndTag, titleStartIdx);
        _name = response[titleStartIdx..titleEndIdx].Trim();
        _logger.Info("Parsed IMDb chart '{0}' name: {1}", _id, _name);

        var descriptionStartIdx = response.IndexOf(s_descriptionBeginTag) + s_descriptionBeginTag.Length;
        var descriptionEndIdx = response.IndexOf(s_descriptionEndTag, descriptionStartIdx);
        _description = response[descriptionStartIdx..descriptionEndIdx].Trim();
        _logger.Info("Parsed IMDb chart '{0}' description: {1}", _id, _description);

        var dataStartIdx = response.IndexOf(s_jsonDataBeginTag) + s_jsonDataBeginTag.Length;
        var dataEndIdx = response.IndexOf(s_jsonDataEndTag, dataStartIdx);
        var jsonData = response[dataStartIdx..dataEndIdx];
        var jsonObject = JsonNode.Parse(jsonData);
        // TODO: better error handling
        var items = jsonObject["props"]["pageProps"]["pageData"]["chartTitles"]["edges"].AsArray();
        _items = items
            .Select((item, idx) => KeyValuePair.Create(item["node"]["id"].ToString(), new ImdbItem(idx + 1, item["node"]["id"].ToString())))
            .ToDictionary(
                item => item.Key,
                item => item.Value);
        _logger.Info("Parsed IMDb chart '{0}' items: {1} items", _id, _items.Count);
    }
}
