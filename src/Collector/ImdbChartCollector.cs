namespace Emby.Plugins.InternetDbCollections.Collector;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugins.InternetDbCollections.Common;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
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

    private static readonly string s_providerId = "imdb";

    private readonly ILogger _logger;
    private readonly ILibraryManager _libraryManager;

    private readonly string _chartId;
    private string _chartName;
    private string _chartDescription;
    private IDictionary<string, ChartItem> _chartItems;

    public ImdbChartCollector(string chartId, ILogger logger, ILibraryManager libraryManager)
    {
        _chartId = chartId;
        _logger = logger;
        _libraryManager = libraryManager;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
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
        _chartName = response[titleStartIdx..titleEndIdx].Trim();

        var descriptionStartIdx = response.IndexOf(s_descriptionBeginTag) + s_descriptionBeginTag.Length;
        var descriptionEndIdx = response.IndexOf(s_descriptionEndTag, descriptionStartIdx);
        _chartDescription = response[descriptionStartIdx..descriptionEndIdx].Trim();

        var dataStartIdx = response.IndexOf(s_jsonDataBeginTag) + s_jsonDataBeginTag.Length;
        var dataEndIdx = response.IndexOf(s_jsonDataEndTag, dataStartIdx);
        var jsonData = response[dataStartIdx..dataEndIdx];
        var jsonObject = JsonNode.Parse(jsonData);
        // TODO: better error handling
        var items = jsonObject["props"]["pageProps"]["pageData"]["chartTitles"]["edges"].AsArray();
        _chartItems = items
            .Select((item, idx) => KeyValuePair.Create(item["node"]["id"].ToString(), new ChartItem(idx + 1)))
            .ToDictionary(
                item => item.Key,
                item => item.Value);
        _logger.Debug("Parsed {0} IMDb chart '{1}' items", _chartItems.Count, _chartId);
    }

    public async Task UpdateMetadataAsync(IProgress<double> progress, CancellationToken cancellationToken = default)
    {
        await CleanupTagsAsync(new ProgressWithBound(progress, 0, 50), cancellationToken);
        await AddTagsAsync(new ProgressWithBound(progress, 50, 100), cancellationToken);
    }

    public Task CleanupMetadataAsync(IProgress<double> progress, CancellationToken cancellationToken = default)
    {
        return CleanupTagsAsync(progress, cancellationToken);
    }

    private Task CleanupTagsAsync(IProgress<double> progress, CancellationToken cancellationToken = default)
    {
        var items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            // MediaTypes = new[] { MediaType.Video },
            IncludeItemTypes = new[] { nameof(Movie), nameof(Series) },
            Tags = new[] { _chartName },
        });
        _logger.Info("Found {0} items with tag '{1}' from library, start to remove the tag from the items", items.Length, _chartName);

        foreach (var (idx, item) in items.Select((item, idx) => (idx, item)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(100.0 * idx / items.Length);
            item.Tags = item.Tags.Where(tag => tag != _chartName).ToArray();
            _libraryManager.UpdateItem(item, item, ItemUpdateType.MetadataEdit);
            _logger.Debug("Remove tag '{0}' from item '{1}' ({2}/{3})", _chartName, item.Name, idx + 1, items.Length);
        }

        _logger.Info("Removed tag '{0}' from {1} items", _chartName, items.Length);
        progress.Report(100);
        return Task.CompletedTask;
    }

    private Task AddTagsAsync(IProgress<double> progress, CancellationToken cancellationToken = default)
    {
        var items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            // MediaTypes = new[] { MediaType.Video },
            IncludeItemTypes = new[] { nameof(Movie), nameof(Series) },
            AnyProviderIdEquals = _chartItems.Keys.Select((imdbId, _) => KeyValuePair.Create(s_providerId, imdbId)).ToList(),
        });
        _logger.Info("Found {0} IMDb Top 250 items in library, start to add tag '{1}'", items.Length, _chartName);

        foreach (var (idx, item) in items.Select((item, idx) => (idx, item)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(100.0 * idx / _chartItems.Count);
            // TODO: Add tag "IMDb Top 250 #" + order? But it will generate lots of tags, and each tag has only one item
            item.Tags = item.Tags.Append(_chartName).ToArray();
            _libraryManager.UpdateItem(item, item, ItemUpdateType.MetadataEdit);
            _logger.Debug("Add tag '{0}' to item '{1}' ({2}/{3})", _chartName, item.Name, idx + 1, items.Length);
        }

        _logger.Info("Added tag '{0}' to {1} items", _chartName, items.Length);
        progress.Report(100);
        return Task.CompletedTask;
    }

    private record struct ChartItem(int Order);
}
