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
            logger.Debug("Tagging is disabled for collector '{0}', skipping tag update", itemList.Name);
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
        List<BaseItem> items = libraryManager.GetItemList(new InternalItemsQuery
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
        logger.Info("Found {0} items need to remove tag '{1}'", items.Count, itemList.Name);

        var updatedItems = new List<BaseItem>();
        foreach (var (idx, item) in items.Select((item, idx) => (idx, item)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(100.0 * idx / items.Count);
            item.Tags = item.Tags.Where(tag => tag != itemList.Name).ToArray();
            updatedItems.Add(item);
            logger.Debug("Remove tag '{0}' from item '{1}' ({2}/{3})", itemList.Name, item.Name, idx + 1, items.Count);
        }
        libraryManager.UpdateItems(updatedItems, null, ItemUpdateType.MetadataEdit, null, cancellationToken);

        logger.Info("Removed tag '{0}' from {1} items", itemList.Name, items.Count);
        progress.Report(100);
        return Task.CompletedTask;
    }

    private async Task AddTagsAsync(
        CollectionItemList itemList,
        IProgress<double> progress,
        bool onlyNotHasTag = true,
        CancellationToken cancellationToken = default)
    {
        var items = await QueryItemsAsync(
            itemList,
            item =>
            {
                if (onlyNotHasTag)
                {
                    item.ExcludeTags = [itemList.Name];
                }
                return item;
            },
            cancellationToken);
        logger.Info("Found {0} items in library, start to add tag '{1}'", items.Count, itemList.Name);

        var updatedItems = new List<BaseItem>();
        foreach (var (idx, item) in items.Select((item, idx) => (idx, item)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(100.0 * idx / items.Count);
            // TODO: Add tag "IMDb Top 250 #" + order? But it will generate lots of tags, and each tag has only one item
            item.Tags = item.Tags.Append(itemList.Name).ToArray();
            updatedItems.Add(item);
            logger.Debug("Add tag '{0}' to item '{1}' ({2}/{3})", itemList.Name, item.Name, idx + 1, items.Count);
        }
        libraryManager.UpdateItems(updatedItems, null, ItemUpdateType.MetadataEdit, null, cancellationToken);

        logger.Info("Added tag '{0}' to {1} items", itemList.Name, items.Count);
        progress.Report(100);
    }

    private async Task UpdateCollectionAsync(
        CollectionItemList itemList,
        IProgress<double> progress,
        CancellationToken cancellationToken = default)
    {
        if (!itemList.EnableCollections)
        {
            logger.Debug("Collection is disabled for collector '{0}', skipping collection update", itemList.Name);
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
        var collections = libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [nameof(BoxSet)],
            Name = itemList.Name,
        }, cancellationToken);
        if (collections.Length == 0)
        {
            logger.Debug("No collection with name '{0}' found, nothing to cleanup", itemList.Name);
            progress.Report(100);
            return Task.CompletedTask;
        }
        if (collections.Length > 1)
        {
            logger.Warn("Found {0} collections with name '{1}', expected 1", collections.Length, itemList.Name);
            progress.Report(100);
            return Task.CompletedTask;
        }

        var collection = collections[0];
        var items = libraryManager.GetItemList(new InternalItemsQuery
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
        logger.Info("Found {0} items need to be removed from the BoxSet '{1}'", items.Count, itemList.Name);

        var updatedItems = new List<BaseItem>();
        foreach (var (idx, item) in items.Select((item, idx) => (idx, item)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(100.0 * idx / items.Count);
            item.RemoveCollection(collection.InternalId);
            updatedItems.Add(item);
            logger.Debug("Remove item '{0}' from collection '{1}' ({2}/{3})", item.Name, itemList.Name, idx + 1, items.Count);
        }
        libraryManager.UpdateItems(updatedItems, null, ItemUpdateType.MetadataEdit, null, cancellationToken);

        logger.Info("Removed {0} items from collection '{1}'", items.Count, itemList.Name);
        progress.Report(100);
        return Task.CompletedTask;
    }

    private async Task AddCollectionAsync(
        CollectionItemList itemList,
        IProgress<double> progress,
        bool onlyNotInCollection = true,
        CancellationToken cancellationToken = default)
    {
        var items = await QueryItemsAsync(itemList, cancellationToken: cancellationToken);
        if (onlyNotInCollection)
        {
            var colls = libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = [nameof(BoxSet)],
                Name = itemList.Name,
            }, cancellationToken);
            if (colls.Length == 1)
            {
                List<BaseItem> itemsInCollection = libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = itemList.Items.Select(item => item.Type).Distinct().ToArray(),
                    CollectionIds = [colls[0].InternalId],
                }, cancellationToken).ToList();
                items = items.Except(itemsInCollection, new BaseItemComparer()).ToList();
            }
            else
            {
                logger.Warn("Found {0} collections with name '{1}', expected 1", colls.Length, itemList.Name);
            }
        }
        logger.Info("Found {0} items in library, start to add them to collection '{1}'", items.Count, itemList.Name);

        var updatedItems = new List<BaseItem>();
        foreach (var (idx, item) in items.Select((item, idx) => (idx, item)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(100.0 * idx / items.Count);
            item.AddCollection(itemList.Name);
            updatedItems.Add(item);
            logger.Debug("Add item '{0}' to collection '{1}' ({2}/{3})", item.Name, itemList.Name, idx + 1, items.Count);
        }
        libraryManager.UpdateItems(updatedItems, null, ItemUpdateType.MetadataEdit, null, cancellationToken);
        logger.Info("Added {0} items to collection '{1}'", items.Count, itemList.Name);

        var collections = libraryManager.GetItemList(new InternalItemsQuery
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
            libraryManager.UpdateItem(collection, collection, ItemUpdateType.MetadataEdit);
            logger.Info("Updated collection '{0}' with description and provider IDs", itemList.Name);
        }
        else
        {
            logger.Warn("Found {0} collections with name '{1}', expected 1", collections.Length, itemList.Name);
        }
        progress.Report(100);
    }

    private async Task<List<BaseItem>> QueryItemsAsync(
        CollectionItemList itemList,
        Func<InternalItemsQuery, InternalItemsQuery>? queryBuilder = null,
        CancellationToken cancellationToken = default)
    {
        var itemTypes = itemList.Items.Select(item => item.Type).Distinct().ToArray();
        var providerIds = itemList.Items.SelectMany(item => item.Ids);
        var queryTasks =
            providerIds.Chunk(100)
            .Select(batch =>
                Task.Run(() =>
                {
                    var query = new InternalItemsQuery()
                    {
                        IncludeItemTypes = itemTypes,
                        AnyProviderIdEquals = batch,
                    };
                    if (queryBuilder != null)
                    {
                        query = queryBuilder(query);
                    }
                    return libraryManager.GetItemList(query, cancellationToken);
                }));
        var items = await Task.WhenAll(queryTasks);
        return items.SelectMany(x => x).ToList();
    }
}
