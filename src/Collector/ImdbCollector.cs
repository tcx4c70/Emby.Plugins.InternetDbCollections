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

    private bool _tag;
    private bool _collection;

    public ImdbCollector(string id, string name, bool tag, bool collection, ILogger logger, ILibraryManager libraryManager)
    {
        _id = id;
        _name = name;
        _tag = tag;
        _collection = collection;
        _logger = logger;
        _libraryManager = libraryManager;
    }

    public abstract string Name { get; }

    public abstract Task InitializeAsync(CancellationToken cancellationToken = default);

    public async Task UpdateMetadataAsync(IProgress<double> progress, CancellationToken cancellationToken = default)
    {
        await UpdateTagsAsync(new ProgressWithBound(progress, 0, 50), cancellationToken);
        await UpdateCollectionAsync(new ProgressWithBound(progress, 50, 100), cancellationToken);
    }

    public async Task CleanupMetadataAsync(IProgress<double> progress, CancellationToken cancellationToken = default)
    {
        await CleanupTagsAsync(new ProgressWithBound(progress, 0, 50), false, cancellationToken);
        await CleanupCollectionAsync(new ProgressWithBound(progress, 50, 100), false, cancellationToken);
    }

    private async Task UpdateTagsAsync(IProgress<double> progress, CancellationToken cancellationToken = default)
    {
        if (!_tag)
        {
            _logger.Debug("Tagging is disabled for collector '{0}', skipping tag update", Name);
            progress.Report(100);
            return;
        }

        await CleanupTagsAsync(new ProgressWithBound(progress, 0, 50), true, cancellationToken);
        await AddTagsAsync(new ProgressWithBound(progress, 50, 100), true, cancellationToken);
    }

    private Task CleanupTagsAsync(IProgress<double> progress, bool onlyNotInCollection = false, CancellationToken cancellationToken = default)
    {
        List<BaseItem> items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            // MediaTypes = new[] { MediaType.Video },
            IncludeItemTypes = new[] { nameof(Movie), nameof(Series) },
            Tags = new[] { _name },
        }).ToList();
        if (onlyNotInCollection)
        {
            items = items
                .Where(item => !item.ProviderIds.ContainsKey(s_providerId) || !_items.ContainsKey(item.ProviderIds[s_providerId]))
                .ToList();
        }
        _logger.Info("Found {0} items need to remove tag '{1}'", items.Count, _name);

        foreach (var (idx, item) in items.Select((item, idx) => (idx, item)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(100.0 * idx / items.Count);
            item.Tags = item.Tags.Where(tag => tag != _name).ToArray();
            _libraryManager.UpdateItem(item, item, ItemUpdateType.MetadataEdit);
            _logger.Debug("Remove tag '{0}' from item '{1}' ({2}/{3})", _name, item.Name, idx + 1, items.Count);
        }

        _logger.Info("Removed tag '{0}' from {1} items", _name, items.Count);
        progress.Report(100);
        return Task.CompletedTask;
    }

    private Task AddTagsAsync(IProgress<double> progress, bool onlyNotInCollection = true, CancellationToken cancellationToken = default)
    {
        var items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            // MediaTypes = new[] { MediaType.Video },
            IncludeItemTypes = new[] { nameof(Movie), nameof(Series) },
            AnyProviderIdEquals = _items.Keys.Select((imdbId, _) => KeyValuePair.Create(s_providerId, imdbId)).ToList(),
            ExcludeTags = onlyNotInCollection ? new [] { _name } : Array.Empty<string>(),
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

    private async Task UpdateCollectionAsync(IProgress<double> progress, CancellationToken cancellationToken = default)
    {
        if (!_collection)
        {
            _logger.Debug("Collection is disabled for collector '{0}', skipping collection update", Name);
            progress.Report(100);
            return;
        }

        await CleanupCollectionAsync(new ProgressWithBound(progress, 0, 50), true, cancellationToken);
        await AddCollectionAsync(new ProgressWithBound(progress, 50, 100), true, cancellationToken);
    }

    private Task CleanupCollectionAsync(IProgress<double> progress, bool onlyNotInCollection = false, CancellationToken cancellationToken = default)
    {
        var collections = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { nameof(BoxSet) },
            Name = _name,
        });
        if (collections.Length == 0)
        {
            _logger.Debug("No collection with name '{0}' found, nothing to cleanup", _name);
            progress.Report(100);
            return Task.CompletedTask;
        }
        if (collections.Length > 1)
        {
            _logger.Warn("Found {0} collections with name '{1}', expected 1", collections.Length, _name);
            progress.Report(100);
            return Task.CompletedTask;
        }

        var collection = collections[0];
        var items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { nameof(Movie), nameof(Series) },
            CollectionIds = new[] { collection.InternalId },
        }).ToList();
        if (onlyNotInCollection)
        {
            items = items
                .Where(item => !item.ProviderIds.ContainsKey(s_providerId) || !_items.ContainsKey(item.ProviderIds[s_providerId]))
                .ToList();
        }
        _logger.Info("Found {0} items need to be removed from the BoxSet '{1}'", items.Count, _name);

        foreach (var (idx, item) in items.Select((item, idx) => (idx, item)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(100.0 * idx / items.Count);
            item.RemoveCollection(collection.InternalId);
            _libraryManager.UpdateItem(item, item, ItemUpdateType.MetadataEdit);
            _logger.Debug("Remove item '{0}' from collection '{1}' ({2}/{3})", item.Name, _name, idx + 1, items.Count);
        }

        _logger.Info("Removed {0} items from collection '{1}'", items.Count, _name);
        progress.Report(100);
        return Task.CompletedTask;
    }

    private Task AddCollectionAsync(IProgress<double> progress, bool onlyNotInCollection = true, CancellationToken cancellationToken = default)
    {
        var items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { nameof(Movie), nameof(Series) },
            AnyProviderIdEquals = _items.Keys.Select((imdbId, _) => KeyValuePair.Create(s_providerId, imdbId)).ToList(),
        }).ToList();
        if (onlyNotInCollection)
        {
            var colls = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { nameof(BoxSet) },
                Name = _name,
            });
            if (colls.Length == 1)
            {
                List<BaseItem> itemsInCollection = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { nameof(Movie), nameof(Series) },
                    CollectionIds = new[] { colls[0].InternalId },
                }).ToList();
                items = items.Except(itemsInCollection, new BaseItemComparer()).ToList();
            }
            else
            {
                _logger.Warn("Found {0} collections with name '{1}', expected 1", colls.Length, _name);
            }
        }
        _logger.Info("Found {0} items in library, start to add them to collection '{1}'", items.Count, _name);

        foreach (var (idx, item) in items.Select((item, idx) => (idx, item)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(100.0 * idx / items.Count);
            item.AddCollection(_name);
            _libraryManager.UpdateItem(item, item, ItemUpdateType.MetadataEdit);
            _logger.Debug("Add item '{0}' to collection '{1}' ({2}/{3})", item.Name, _name, idx + 1, items.Count);
        }
        _logger.Info("Added {0} items to collection '{1}'", items.Count, _name);

        var collections = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { nameof(BoxSet) },
            Name = _name,
        });
        if (collections.Length == 1)
        {
            BaseItem collection = collections[0];
            collection.Overview = _description;
            _libraryManager.UpdateItem(collection, collection, ItemUpdateType.MetadataEdit);
            _logger.Info("Updated collection '{0}' with description", _name);
        }
        else
        {
            _logger.Warn("Found {0} collections with name '{1}', expected 1", collections.Length, _name);
        }
        progress.Report(100);
        return Task.CompletedTask;
    }

    protected record struct ImdbItem(int Order, string ItemId);
}
