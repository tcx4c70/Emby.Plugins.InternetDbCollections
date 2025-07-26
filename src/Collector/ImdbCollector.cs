namespace Emby.Plugins.InternetDbCollections.Collector;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugins.InternetDbCollections.Common;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;

abstract class ImdbCollector : ICollector
{
    private static readonly string s_providerId = "imdb";

    protected readonly ILogger _logger;
    protected readonly ILibraryManager _libraryManager;

    protected readonly string _id;
    protected string _name;
    protected string _description;
    protected IDictionary<string, ImdbItem> _items;

    public ImdbCollector(string id, ILogger logger, ILibraryManager libraryManager)
    {
        _id = id;
        _logger = logger;
        _libraryManager = libraryManager;
    }

    public abstract string Name { get; }

    public abstract Task InitializeAsync(CancellationToken cancellationToken = default);

    public async Task UpdateMetadataAsync(IProgress<double> progress, CancellationToken cancellationToken = default)
    {
        await CleanupTagsAsync(new ProgressWithBound(progress, 0, 50), cancellationToken);
        await AddTagsAsync(new ProgressWithBound(progress, 50, 100), cancellationToken);
    }

    public async Task CleanupMetadataAsync(IProgress<double> progress, CancellationToken cancellationToken = default)
    {
        await CleanupTagsAsync(progress, cancellationToken);
    }

    private Task CleanupTagsAsync(IProgress<double> progress, CancellationToken cancellationToken = default)
    {
        var items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            // MediaTypes = new[] { MediaType.Video },
            IncludeItemTypes = new[] { nameof(Movie), nameof(Series) },
            Tags = new[] { _name },
        });
        _logger.Info("Found {0} items with tag '{1}' from library, start to remove the tag from the items", items.Length, _name);

        foreach (var (idx, item) in items.Select((item, idx) => (idx, item)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(100.0 * idx / items.Length);
            item.Tags = item.Tags.Where(tag => tag != _name).ToArray();
            _libraryManager.UpdateItem(item, item, ItemUpdateType.MetadataEdit);
            _logger.Debug("Remove tag '{0}' from item '{1}' ({2}/{3})", _name, item.Name, idx + 1, items.Length);
        }

        _logger.Info("Removed tag '{0}' from {1} items", _name, items.Length);
        progress.Report(100);
        return Task.CompletedTask;
    }

    private Task AddTagsAsync(IProgress<double> progress, CancellationToken cancellationToken = default)
    {
        var items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            // MediaTypes = new[] { MediaType.Video },
            IncludeItemTypes = new[] { nameof(Movie), nameof(Series) },
            AnyProviderIdEquals = _items.Keys.Select((imdbId, _) => KeyValuePair.Create(s_providerId, imdbId)).ToList(),
        });
        _logger.Info("Found {0} items in library, start to add tag '{1}'", items.Length, _name);

        foreach (var (idx, item) in items.Select((item, idx) => (idx, item)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(100.0 * idx / _items.Count);
            // TODO: Add tag "IMDb Top 250 #" + order? But it will generate lots of tags, and each tag has only one item
            item.Tags = item.Tags.Append(_name).ToArray();
            _libraryManager.UpdateItem(item, item, ItemUpdateType.MetadataEdit);
            _logger.Debug("Add tag '{0}' to item '{1}' ({2}/{3})", _name, item.Name, idx + 1, items.Length);
        }

        _logger.Info("Added tag '{0}' to {1} items", _name, items.Length);
        progress.Report(100);
        return Task.CompletedTask;
    }

    protected record struct ImdbItem(int Order, string ItemId);
}
