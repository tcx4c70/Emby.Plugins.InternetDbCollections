namespace Emby.Plugins.InternetDbCollections.Common;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;

class ImdbTop250
{
    private static readonly string s_tagName = "IMDb Top 250";
    private static readonly string s_providerId = "imdb";

    private static readonly string s_imdbTop250Url = "https://www.imdb.com/chart/top/";
    private static readonly string s_jsonDataBeginTag = "<script id=\"__NEXT_DATA__\" type=\"application/json\">";
    private static readonly string s_jsonDataEndTag = "</script>";

    private readonly ILogger _logger;
    private readonly ILibraryManager _libraryManager;

    public ImdbTop250(ILogger logger, ILibraryManager libraryManager)
    {
        _logger = logger;
        _libraryManager = libraryManager;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ImdbTop250Items = await GetImdbTop250IdsAsync(cancellationToken);
    }

    public async Task UpdateTagsAsync(IProgress<double> progress, CancellationToken cancellationToken = default)
    {
        await CleanupOldTagsAsync(new ProgressWithBound(progress, 0, 50), cancellationToken);
        await AddNewTagsAsync(new ProgressWithBound(progress, 50, 100), cancellationToken);
    }

    // TODO: Update IMDb Top 250 collection?

    private Task CleanupOldTagsAsync(IProgress<double> progress, CancellationToken cancellationToken = default)
    {
        var items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            MediaTypes = new[] { MediaType.Video },
            IncludeItemTypes = new[] { nameof(Movie) },
            Tags = new[] { s_tagName },
        });
        _logger.Info("Found {0} items with tag '{1}' from library, start to remove the tag from the items", items.Length, s_tagName);

        foreach (var (idx, item) in items.Select((item, idx) => (idx, item)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(100.0 * idx / items.Length);
            item.Tags = item.Tags.Where(tag => tag != s_tagName).ToArray();
            _libraryManager.UpdateItem(item, item, ItemUpdateType.MetadataEdit);
            _logger.Debug("Remove tag '{0}' from item '{1}' ({2}/{3})", s_tagName, item.Name, idx + 1, items.Length);
        }

        _logger.Info("Removed tag '{0}' from {1} items", s_tagName, items.Length);
        progress.Report(100);
        return Task.CompletedTask;
    }

    private Task AddNewTagsAsync(IProgress<double> progress, CancellationToken cancellationToken = default)
    {
        var items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            MediaTypes = new[] { MediaType.Video },
            IncludeItemTypes = new[] { nameof(Movie) },
            AnyProviderIdEquals = ImdbTop250Items.Select(item => KeyValuePair.Create(s_providerId, item.id)).ToList(),
        });
        _logger.Info("Found {0} IMDb Top 250 items in library, start to add tag '{1}'", items.Length, s_tagName);

        foreach (var (idx, item) in items.Select((item, idx) => (idx, item)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(100.0 * idx / ImdbTop250Items.Count());
            // TODO: Add tag "IMDb Top 250 #" + order? But it will generate lots of tags, and each tag has only one item
            item.Tags = item.Tags.Append(s_tagName).ToArray();
            _libraryManager.UpdateItem(item, item, ItemUpdateType.MetadataEdit);
            _logger.Debug("Add tag '{0}' to item '{1}' ({2}/{3})", s_tagName, item.Name, idx + 1, items.Length);
        }

        _logger.Info("Added tag '{0}' to {1} items", s_tagName, items.Length);
        progress.Report(100);
        return Task.CompletedTask;
    }

    private async Task<IEnumerable<(int, string)>> GetImdbTop250IdsAsync(CancellationToken cancellationToken)
    {
        // TODO:
        // 1. retry
        // 2. proxy
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

        _logger.Debug("Fetching IMDb Top 250 data from {0}", s_imdbTop250Url);
        var response = await client.GetStringAsync(s_imdbTop250Url, cancellationToken);
        _logger.Debug("Received IMDb Top 250 data, parsing...");

        var startIndex = response.IndexOf(s_jsonDataBeginTag) + s_jsonDataBeginTag.Length;
        var endIndex = response.IndexOf(s_jsonDataEndTag, startIndex);
        var jsonData = response[startIndex..endIndex];
        var jsonObject = JsonNode.Parse(jsonData);

        // TODO: better error handling
        var items = jsonObject["props"]["pageProps"]["pageData"]["chartTitles"]["edges"].AsArray();
        var imdbTop250Items = items.Select((item, idx) => (idx + 1, item["node"]["id"].ToString())).ToList();
        _logger.Debug("Parsed {0} IMDb Top 250 items", imdbTop250Items.Count);
        return imdbTop250Items;
    }

    private IEnumerable<(int order, string id)> ImdbTop250Items;
}
