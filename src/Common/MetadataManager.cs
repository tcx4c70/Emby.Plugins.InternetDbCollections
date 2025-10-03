using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugins.InternetDbCollections.Models.Collection;
using Emby.Plugins.InternetDbCollections.Utils;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;

namespace Emby.Plugins.InternetDbCollections.Common;

public class MetadataManager(ILogger logger, ILibraryManager libraryManager)
{
    private readonly ILogger _logger = logger;
    private readonly ILibraryManager _libraryManager = libraryManager;

    public async Task UpdateMetadataAsync(
        CollectionItemList itemList,
        IProgress<double> progress,
        CancellationToken cancellationToken = default)
    {
        await UpdateTagsAsync(itemList, new ProgressWithBound(progress, 0, 50), cancellationToken);
        await UpdateCollectionAsync(itemList, new ProgressWithBound(progress, 50, 100), cancellationToken);
    }

    public async Task CleanupMetadataAsync(
        CollectionItemList itemList,
        IProgress<double> progress,
        CancellationToken cancellationToken = default)
    {
        await CleanupTagsAsync(itemList, new ProgressWithBound(progress, 0, 50), false, cancellationToken);
        await CleanupCollectionAsync(itemList, new ProgressWithBound(progress, 50, 100), false, cancellationToken);
    }

    private async Task UpdateTagsAsync(
        CollectionItemList itemList,
        IProgress<double> progress,
        CancellationToken cancellationToken = default)
    {
        if (!itemList.EnableTags)
        {
            _logger.Debug("Tagging is disabled for collector '{0}', skipping tag update", itemList.Name);
            progress.Report(100);
            return;
        }

        await CleanupTagsAsync(itemList, new ProgressWithBound(progress, 0, 50), true, cancellationToken);
        await AddTagsAsync(itemList, new ProgressWithBound(progress, 50, 100), true, cancellationToken);
    }

    private Task CleanupTagsAsync(
        CollectionItemList itemList,
        IProgress<double> progress,
        bool onlyNotInItemList = false,
        CancellationToken cancellationToken = default)
    {
        List<BaseItem> items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            // MediaTypes = new[] { MediaType.Video },
            IncludeItemTypes = itemList.Items.Select(item => item.Type).Distinct().ToArray(),
            Tags = [itemList.Name],
        }, cancellationToken).ToList();
        if (onlyNotInItemList)
        {
            var providerIds =
                itemList.Items
                .SelectMany(item => item.Ids.Select(kvp => (kvp.Key.ToLower(), kvp.Value.ToLower())))
                .ToHashSet();
            items = items
                .Where(item =>
                    item.ProviderIds
                    .All(kvp => !providerIds.Contains((kvp.Key.ToLower(), kvp.Value.ToLower()))))
                .ToList();
        }
        _logger.Info("Found {0} items need to remove tag '{1}'", items.Count, itemList.Name);

        foreach (var (idx, item) in items.Select((item, idx) => (idx, item)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(100.0 * idx / items.Count);
            item.Tags = item.Tags.Where(tag => tag != itemList.Name).ToArray();
            _libraryManager.UpdateItem(item, item, ItemUpdateType.MetadataEdit);
            _logger.Debug("Remove tag '{0}' from item '{1}' ({2}/{3})", itemList.Name, item.Name, idx + 1, items.Count);
        }

        _logger.Info("Removed tag '{0}' from {1} items", itemList.Name, items.Count);
        progress.Report(100);
        return Task.CompletedTask;
    }

    private Task AddTagsAsync(
        CollectionItemList itemList,
        IProgress<double> progress,
        bool onlyNotHasTag = true,
        CancellationToken cancellationToken = default)
    {
        var items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            // MediaTypes = new[] { MediaType.Video },
            IncludeItemTypes = itemList.Items.Select(item => item.Type).Distinct().ToArray(),
            AnyProviderIdEquals = itemList.Items.SelectMany(item => item.Ids).ToList(),
            ExcludeTags = onlyNotHasTag ? [itemList.Name] : [],
        }, cancellationToken);
        _logger.Info("Found {0} items in library, start to add tag '{1}'", items.Length, itemList.Name);

        foreach (var (idx, item) in items.Select((item, idx) => (idx, item)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(100.0 * idx / items.Length);
            // TODO: Add tag "IMDb Top 250 #" + order? But it will generate lots of tags, and each tag has only one item
            item.Tags = item.Tags.Append(itemList.Name).ToArray();
            _libraryManager.UpdateItem(item, item, ItemUpdateType.MetadataEdit);
            _logger.Debug("Add tag '{0}' to item '{1}' ({2}/{3})", itemList.Name, item.Name, idx + 1, items.Length);
        }

        _logger.Info("Added tag '{0}' to {1} items", itemList.Name, items.Length);
        progress.Report(100);
        return Task.CompletedTask;
    }

    private async Task UpdateCollectionAsync(
        CollectionItemList itemList,
        IProgress<double> progress,
        CancellationToken cancellationToken = default)
    {
        if (!itemList.EnableCollections)
        {
            _logger.Debug("Collection is disabled for collector '{0}', skipping collection update", itemList.Name);
            progress.Report(100);
            return;
        }

        await CleanupCollectionAsync(itemList, new ProgressWithBound(progress, 0, 50), true, cancellationToken);
        await AddCollectionAsync(itemList, new ProgressWithBound(progress, 50, 100), true, cancellationToken);
    }

    private Task CleanupCollectionAsync(
        CollectionItemList itemList,
        IProgress<double> progress,
        bool onlyNotInItemList = false,
        CancellationToken cancellationToken = default)
    {
        var collections = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [nameof(BoxSet)],
            Name = itemList.Name,
        }, cancellationToken);
        if (collections.Length == 0)
        {
            _logger.Debug("No collection with name '{0}' found, nothing to cleanup", itemList.Name);
            progress.Report(100);
            return Task.CompletedTask;
        }
        if (collections.Length > 1)
        {
            _logger.Warn("Found {0} collections with name '{1}', expected 1", collections.Length, itemList.Name);
            progress.Report(100);
            return Task.CompletedTask;
        }

        var collection = collections[0];
        var items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = itemList.Items.Select(item => item.Type).Distinct().ToArray(),
            CollectionIds = [collection.InternalId],
        }, cancellationToken).ToList();
        if (onlyNotInItemList)
        {
            var providerIds =
                itemList.Items
                .SelectMany(item => item.Ids.Select(kvp => (kvp.Key.ToLower(), kvp.Value.ToLower())))
                .ToHashSet();
            items = items
                .Where(item =>
                    item.ProviderIds
                    .All(kvp => !providerIds.Contains((kvp.Key.ToLower(), kvp.Value.ToLower()))))
                .ToList();
        }
        _logger.Info("Found {0} items need to be removed from the BoxSet '{1}'", items.Count, itemList.Name);

        foreach (var (idx, item) in items.Select((item, idx) => (idx, item)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(100.0 * idx / items.Count);
            item.RemoveCollection(collection.InternalId);
            _libraryManager.UpdateItem(item, item, ItemUpdateType.MetadataEdit);
            _logger.Debug("Remove item '{0}' from collection '{1}' ({2}/{3})", item.Name, itemList.Name, idx + 1, items.Count);
        }

        _logger.Info("Removed {0} items from collection '{1}'", items.Count, itemList.Name);
        progress.Report(100);
        return Task.CompletedTask;
    }

    private Task AddCollectionAsync(
        CollectionItemList itemList,
        IProgress<double> progress,
        bool onlyNotInCollection = true,
        CancellationToken cancellationToken = default)
    {
        var items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = itemList.Items.Select(item => item.Type).Distinct().ToArray(),
            AnyProviderIdEquals = itemList.Items.SelectMany(item => item.Ids).ToList(),
        }, cancellationToken).ToList();
        if (onlyNotInCollection)
        {
            var colls = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = [nameof(BoxSet)],
                Name = itemList.Name,
            }, cancellationToken);
            if (colls.Length == 1)
            {
                List<BaseItem> itemsInCollection = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = itemList.Items.Select(item => item.Type).Distinct().ToArray(),
                    CollectionIds = [colls[0].InternalId],
                }, cancellationToken).ToList();
                items = items.Except(itemsInCollection, new BaseItemComparer()).ToList();
            }
            else
            {
                _logger.Warn("Found {0} collections with name '{1}', expected 1", colls.Length, itemList.Name);
            }
        }
        _logger.Info("Found {0} items in library, start to add them to collection '{1}'", items.Count, itemList.Name);

        foreach (var (idx, item) in items.Select((item, idx) => (idx, item)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(100.0 * idx / items.Count);
            item.AddCollection(itemList.Name);
            _libraryManager.UpdateItem(item, item, ItemUpdateType.MetadataEdit);
            _logger.Debug("Add item '{0}' to collection '{1}' ({2}/{3})", item.Name, itemList.Name, idx + 1, items.Count);
        }
        _logger.Info("Added {0} items to collection '{1}'", items.Count, itemList.Name);

        var collections = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [nameof(BoxSet)],
            Name = itemList.Name,
        }, cancellationToken);
        if (collections.Length == 1)
        {
            BaseItem collection = collections[0];
            if (!collection.IsFieldLocked(MetadataFields.Overview))
            {
                collection.Overview = itemList.Description ?? "";
            }

            foreach (var (name, id) in itemList.Ids)
            {
                collection.SetProviderId(name, id);
            }
            _libraryManager.UpdateItem(collection, collection, ItemUpdateType.MetadataEdit);
            _logger.Info("Updated collection '{0}' with description and provider IDs", itemList.Name);
        }
        else
        {
            _logger.Warn("Found {0} collections with name '{1}', expected 1", collections.Length, itemList.Name);
        }
        progress.Report(100);
        return Task.CompletedTask;
    }
}
