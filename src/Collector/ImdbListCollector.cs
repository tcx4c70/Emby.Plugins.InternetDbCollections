namespace Emby.Plugins.InternetDbCollections.Collector;

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;

class ImdbListCollector : ImdbCollector
{
    private static readonly string s_imdbListUrlTemplate = "https://www.imdb.com/list/{0}/";
    private static readonly string s_jsonDataBeginTag = "<script id=\"__NEXT_DATA__\" type=\"application/json\">";
    private static readonly string s_jsonDataEndTag = "</script>";

    public ImdbListCollector(string listId, ILogger logger, ILibraryManager libraryManager)
        : base(listId, logger, libraryManager)
    {
    }

    public override string Name => $"IMDb List: {_name ?? _id}";

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

        var url = string.Format(s_imdbListUrlTemplate, _id);
        _logger.Debug("Fetching IMDb list '{0}' data from {1}", _id, url);
        var response = await client.GetStringAsync(url, cancellationToken);
        _logger.Debug("Received IMDb list '{0}' data, parsing...", _id);

        var dataStartIdx = response.IndexOf(s_jsonDataBeginTag) + s_jsonDataBeginTag.Length;
        var dataEndIdx = response.IndexOf(s_jsonDataEndTag, dataStartIdx);
        var jsonData = response[dataStartIdx..dataEndIdx];
        var jsonObject = JsonNode.Parse(jsonData);
        _name = jsonObject["props"]["pageProps"]["mainColumnData"]["list"]["name"]["originalText"].ToString();
        _logger.Info("Parsed IMDb list name: {0}", _name);
        _description = jsonObject["props"]["pageProps"]["mainColumnData"]["list"]["description"]["originalText"]["plainText"].ToString();
        _logger.Info("Parsed IMDb list description: {0}", _description);

        var items = jsonObject["props"]["pageProps"]["mainColumnData"]["list"]["titleListItemSearch"]["edges"].AsArray();
        _items = items
            .Select((item, idx) => KeyValuePair.Create(item["listItem"]["id"].ToString(), new ImdbItem(idx + 1, item["listItem"]["id"].ToString())))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        _logger.Info("Parsed IMDb list items: {0} items", _items.Count);
    }
}
