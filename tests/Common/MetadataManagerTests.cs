using System.Collections;
using System.Reflection;
using Emby.Plugins.InternetDbCollections.Common;
using Emby.Plugins.InternetDbCollections.Models.Collection;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using Moq;

namespace Emby.Plugins.InternetDbCollections.Tests;

public class MetadataManagerTests
{
    [Fact]
    public async Task UpdateMetadataAsync_adds_tags_and_removes_only_items_not_represented_by_provider_ids()
    {
        var logger = Mock.Of<ILogger>();
        var queries = new List<InternalItemsQuery>();
        var updatedBatches = new List<List<BaseItem>>();
        var libraryManager = new Mock<ILibraryManager>(MockBehavior.Strict);
        long[] libraryIds = [11L, 22L];

        var itemList = CreateItemList(
            "IMDb Top 250",
            enableTags: true,
            enableCollections: false,
            CreateCollectionItem(1, nameof(Movie), "tt0111161"),
            CreateCollectionItem(2, nameof(Series), "tt0903747"));

        var cleanupMatch = CreateMovie(1, "Cleanup match", "tt0111161", itemList.Name, "keep");
        var cleanupOrphan = CreateMovie(2, "Cleanup orphan", "tt9999999", itemList.Name, "keep");
        var addMovie = CreateMovie(3, "Add movie", "tt0111161", "keep");
        var addSeries = CreateSeries(4, "Add series", "tt0903747", "keep");

        SetupGetItemList(
            libraryManager,
            queries,
            query =>
            {
                if (query.Tags?.Contains(itemList.Name) == true)
                {
                    return [cleanupMatch, cleanupOrphan];
                }

                if (query.AnyProviderIdEquals?.Any() == true)
                {
                    return [addMovie, addSeries];
                }

                throw new InvalidOperationException($"Unexpected query: {Describe(query)}");
            });

        SetupUpdateItems(libraryManager, updatedBatches);

        var manager = new MetadataManager(logger, libraryManager.Object, libraryIds);

        await manager.UpdateMetadataAsync(itemList, new RecordingProgress<double>());

        Assert.Equal(2, updatedBatches.Count);
        Assert.Single(updatedBatches[0]);
        Assert.Equal("Cleanup orphan", updatedBatches[0][0].Name);
        Assert.Equal(2, updatedBatches[1].Count);
        Assert.All(updatedBatches[1], item => Assert.Contains(itemList.Name, item.Tags));
        Assert.Contains(itemList.Name, cleanupMatch.Tags);
        Assert.DoesNotContain(itemList.Name, cleanupOrphan.Tags);
        Assert.Contains(itemList.Name, addMovie.Tags);
        Assert.Contains(itemList.Name, addSeries.Tags);

        Assert.Equal(2, queries.Count);
        Assert.Equal([nameof(Movie), nameof(Series)], queries[0].IncludeItemTypes);
        Assert.Equal([itemList.Name], queries[0].Tags);
        Assert.Empty(queries[0].ExcludeTags);
        Assert.True(queries[0].ParentIds == null || queries[0].ParentIds.Length == 0);
        Assert.Equal([nameof(Movie), nameof(Series)], queries[1].IncludeItemTypes);
        Assert.Equal([itemList.Name], queries[1].ExcludeTags);
        Assert.Equal(2, queries[1].AnyProviderIdEquals!.Count);
        Assert.Equal(libraryIds, queries[1].ParentIds);
    }

    [Fact]
    public async Task UpdateMetadataAsync_adds_items_to_collection_and_updates_collection_metadata()
    {
        var logger = Mock.Of<ILogger>();
        var queries = new List<InternalItemsQuery>();
        var updatedBatches = new List<List<BaseItem>>();
        BoxSet? updatedCollection = null;
        var libraryManager = new Mock<ILibraryManager>(MockBehavior.Strict);
        long[] libraryIds = [11L, 22L];

        var itemList = CreateItemList(
            "IMDb Top 250",
            enableTags: false,
            enableCollections: true,
            CreateCollectionItem(1, nameof(Movie), "tt0111161"),
            CreateCollectionItem(2, nameof(Series), "tt0903747"));
        itemList.Description = "Top rated movies on IMDb.";
        itemList.Ids = new Dictionary<string, string>
        {
            ["imdb"] = "top",
            ["tmdb"] = "1234",
        };

        var collection = CreateBoxSet(100, itemList.Name);
        var cleanupOrphan = CreateMovie(3, "Cleanup orphan", "tt8888888");
        var alreadyInCollection = CreateMovie(4, "Already in collection", "tt0111161");
        var newItem = CreateSeries(5, "New item", "tt0903747");

        SetupGetItemList(
            libraryManager,
            queries,
            query =>
            {
                if (query.IncludeItemTypes?.Contains(nameof(BoxSet)) == true)
                {
                    return [collection];
                }

                if (query.CollectionIds?.Any() == true)
                {
                    return [alreadyInCollection, cleanupOrphan];
                }

                if (query.AnyProviderIdEquals?.Any() == true)
                {
                    return [alreadyInCollection, newItem];
                }

                throw new InvalidOperationException($"Unexpected query: {Describe(query)}");
            });

        SetupUpdateItems(libraryManager, updatedBatches);
        libraryManager.Setup(m => m.UpdateItem(It.IsAny<BaseItem>(), It.IsAny<BaseItem>(), ItemUpdateType.MetadataEdit))
            .Callback<BaseItem, BaseItem, ItemUpdateType>((item, _, _) => updatedCollection = (BoxSet)item);

        var manager = new MetadataManager(logger, libraryManager.Object, libraryIds);

        await manager.UpdateMetadataAsync(itemList, new RecordingProgress<double>());

        Assert.Equal(2, updatedBatches.Count);
        Assert.Single(updatedBatches[0]);
        Assert.Equal("Cleanup orphan", updatedBatches[0][0].Name);
        Assert.Single(updatedBatches[1]);
        Assert.Equal("New item", updatedBatches[1][0].Name);
        Assert.Same(collection, updatedCollection);
        Assert.Equal(itemList.Description, updatedCollection!.Overview);
        Assert.Equal("top", updatedCollection.ProviderIds["imdb"]);
        Assert.Equal("1234", updatedCollection.ProviderIds["tmdb"]);

        Assert.Equal(6, queries.Count);
        Assert.Equal(3, queries.Count(query => query.IncludeItemTypes?.Contains(nameof(BoxSet)) == true));
        Assert.Equal(2, queries.Count(query => query.CollectionIds?.Any() == true));
        Assert.Equal(1, queries.Count(query => query.AnyProviderIdEquals?.Any() == true));
        Assert.All(queries.Take(2), query => Assert.True(query.ParentIds == null || query.ParentIds.Length == 0));
        Assert.Equal(libraryIds, queries.Single(query => query.AnyProviderIdEquals?.Any() == true).ParentIds);
        Assert.All(
            queries.Where(query => query.AnyProviderIdEquals?.Any() != true),
            query => Assert.True(query.ParentIds == null || query.ParentIds.Length == 0));
    }

    [Fact]
    public async Task CleanupMetadataAsync_removes_tags_and_collection_memberships_without_provider_id_filtering()
    {
        var logger = Mock.Of<ILogger>();
        var queries = new List<InternalItemsQuery>();
        var updatedBatches = new List<List<BaseItem>>();
        var libraryManager = new Mock<ILibraryManager>(MockBehavior.Strict);
        long[] libraryIds = [11L, 22L];

        var itemList = CreateItemList(
            "Cleanup list",
            enableTags: false,
            enableCollections: false,
            CreateCollectionItem(1, nameof(Movie), "tt0111161"),
            CreateCollectionItem(2, nameof(Series), "tt0903747"));

        var tagMatch = CreateMovie(1, "Tag match", "tt0111161", itemList.Name, "keep");
        var tagOrphan = CreateMovie(2, "Tag orphan", "tt9999999", itemList.Name, "keep");
        var collection = CreateBoxSet(100, itemList.Name);
        var collectionMatch = CreateMovie(3, "Collection match", "tt0111161");
        var collectionOrphan = CreateSeries(4, "Collection orphan", "tt9999999");

        SetupGetItemList(
            libraryManager,
            queries,
            query =>
            {
                if (query.Tags?.Contains(itemList.Name) == true)
                {
                    return [tagMatch, tagOrphan];
                }

                if (query.IncludeItemTypes?.Contains(nameof(BoxSet)) == true)
                {
                    return [collection];
                }

                if (query.CollectionIds?.Any() == true)
                {
                    return [collectionMatch, collectionOrphan];
                }

                throw new InvalidOperationException($"Unexpected query: {Describe(query)}");
            });

        SetupUpdateItems(libraryManager, updatedBatches);

        var manager = new MetadataManager(logger, libraryManager.Object, libraryIds);

        await manager.CleanupMetadataAsync(itemList, new RecordingProgress<double>());

        Assert.Equal(2, updatedBatches.Count);
        Assert.Equal(2, updatedBatches[0].Count);
        Assert.Equal(2, updatedBatches[1].Count);
        Assert.DoesNotContain(itemList.Name, tagMatch.Tags);
        Assert.DoesNotContain(itemList.Name, tagOrphan.Tags);
        Assert.Contains(updatedBatches[1], item => item.Name == "Collection match");
        Assert.Contains(updatedBatches[1], item => item.Name == "Collection orphan");

        Assert.Equal(3, queries.Count);
        Assert.Equal(1, queries.Count(query => query.Tags?.Any() == true));
        Assert.Equal(1, queries.Count(query => query.IncludeItemTypes?.Contains(nameof(BoxSet)) == true));
        Assert.Equal(1, queries.Count(query => query.CollectionIds?.Any() == true));
        Assert.All(queries, query => Assert.True(query.ParentIds == null || query.ParentIds.Length == 0));
    }

    [Fact]
    public async Task UpdateMetadataAsync_skips_both_sections_when_disabled()
    {
        var logger = Mock.Of<ILogger>();
        var libraryManager = new Mock<ILibraryManager>(MockBehavior.Strict);
        var manager = new MetadataManager(logger, libraryManager.Object, []);
        var itemList = CreateItemList("Disabled list", enableTags: false, enableCollections: false);
        var progress = new RecordingProgress<double>();

        await manager.UpdateMetadataAsync(itemList, progress);

        Assert.Equal([50, 100], progress.Values);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task CleanupMetadataAsync_skips_collection_cleanup_when_collection_count_is_not_one(int collectionCount)
    {
        var logger = Mock.Of<ILogger>();
        var queries = new List<InternalItemsQuery>();
        var updatedBatches = new List<List<BaseItem>>();
        var libraryManager = new Mock<ILibraryManager>(MockBehavior.Strict);
        long[] libraryIds = [11L, 22L];

        var itemList = CreateItemList(
            "Ambiguous list",
            enableTags: false,
            enableCollections: false,
            CreateCollectionItem(1, nameof(Movie), "tt0111161"));

        SetupGetItemList(
            libraryManager,
            queries,
            query =>
            {
                if (query.Tags?.Any() == true)
                {
                    return [];
                }

                if (query.IncludeItemTypes?.Contains(nameof(BoxSet)) == true)
                {
                    return Enumerable.Range(1, collectionCount)
                        .Select(index => (BaseItem)CreateBoxSet(index, itemList.Name))
                        .ToArray();
                }

                throw new InvalidOperationException($"Unexpected query: {Describe(query)}");
            });

        SetupUpdateItems(libraryManager, updatedBatches);

        var manager = new MetadataManager(logger, libraryManager.Object, libraryIds);

        await manager.CleanupMetadataAsync(itemList, new RecordingProgress<double>());

        Assert.Single(updatedBatches);
        Assert.Empty(updatedBatches[0]);
        Assert.Equal(2, queries.Count);
        Assert.Equal(1, queries.Count(query => query.Tags?.Any() == true));
        Assert.Equal(1, queries.Count(query => query.IncludeItemTypes?.Contains(nameof(BoxSet)) == true));
        Assert.DoesNotContain(queries, query => query.CollectionIds?.Any() == true);
        Assert.All(queries, query => Assert.True(query.ParentIds == null || query.ParentIds.Length == 0));
    }

    [Fact]
    public async Task UpdateMetadataAsync_chunks_provider_ids_across_multiple_queries()
    {
        var logger = Mock.Of<ILogger>();
        var queries = new List<InternalItemsQuery>();
        var updatedBatches = new List<List<BaseItem>>();
        var libraryManager = new Mock<ILibraryManager>(MockBehavior.Strict);
        long[] libraryIds = [11L, 22L];

        var itemListItems = Enumerable.Range(1, 101)
            .Select(index => CreateCollectionItem(index, nameof(Movie), $"tt{index:07}"))
            .ToArray();
        var itemList = CreateItemList("Chunked list", enableTags: true, enableCollections: false, itemListItems);

        var libraryItems = Enumerable.Range(1, 101)
            .Select(index => (BaseItem)CreateMovie(index, $"Movie {index}", $"tt{index:07}", "existing"))
            .ToArray();

        SetupGetItemList(
            libraryManager,
            queries,
            query =>
            {
                if (query.Tags?.Any() == true)
                {
                    return [];
                }

                if (query.AnyProviderIdEquals?.Any() == true)
                {
                    return libraryItems.Where(item => MatchesProviderIds(item, query.AnyProviderIdEquals)).ToArray();
                }

                throw new InvalidOperationException($"Unexpected query: {Describe(query)}");
            });

        SetupUpdateItems(libraryManager, updatedBatches);

        var manager = new MetadataManager(logger, libraryManager.Object, libraryIds);

        await manager.UpdateMetadataAsync(itemList, new RecordingProgress<double>());

        Assert.Equal(2, updatedBatches.Count);
        Assert.Empty(updatedBatches[0]);
        Assert.Equal(101, updatedBatches[1].Count);
        Assert.All(updatedBatches[1], item => Assert.Contains(itemList.Name, item.Tags));
        Assert.Equal(2, queries.Count(query => query.AnyProviderIdEquals?.Any() == true));
        Assert.Contains(queries.Where(query => query.AnyProviderIdEquals?.Any() == true), query => query.AnyProviderIdEquals!.Count == 100);
        Assert.Contains(queries.Where(query => query.AnyProviderIdEquals?.Any() == true), query => query.AnyProviderIdEquals!.Count == 1);
        Assert.True(queries[0].ParentIds == null || queries[0].ParentIds.Length == 0);
        Assert.All(queries.Skip(1), query => Assert.Equal(libraryIds, query.ParentIds));
    }

    private static void SetupGetItemList(
        Mock<ILibraryManager> libraryManager,
        List<InternalItemsQuery> queries,
        Func<InternalItemsQuery, BaseItem[]> handler)
    {
        libraryManager
            .Setup(manager => manager.GetItemList(It.IsAny<InternalItemsQuery>(), It.IsAny<CancellationToken>()))
            .Returns((InternalItemsQuery query, CancellationToken _) =>
            {
                lock (queries)
                {
                    queries.Add(query);
                }

                return handler(query);
            });
    }

    private static void SetupUpdateItems(Mock<ILibraryManager> libraryManager, List<List<BaseItem>> updatedBatches)
    {
        libraryManager
            .Setup(manager => manager.UpdateItems(It.IsAny<List<BaseItem>>(), It.IsAny<BaseItem>(), ItemUpdateType.MetadataEdit, It.IsAny<MetadataRefreshOptions>(), It.IsAny<CancellationToken>()))
            .Callback<List<BaseItem>, BaseItem, ItemUpdateType, MetadataRefreshOptions, CancellationToken>((items, _, _, _, _) =>
            {
                updatedBatches.Add(items.ToList());
            });
    }

    private static CollectionItemList CreateItemList(string name, bool enableTags, bool enableCollections, params CollectionItem[] items)
    {
        return new CollectionItemList
        {
            Name = name,
            EnableTags = enableTags,
            EnableCollections = enableCollections,
            Items = items,
        };
    }

    private static CollectionItem CreateCollectionItem(int order, string type, string imdbId)
    {
        return new CollectionItem
        {
            Order = order,
            Type = type,
            Ids = new Dictionary<string, string>
            {
                ["imdb"] = imdbId,
            },
        };
    }

    private static Movie CreateMovie(long internalId, string name, string imdbId, params string[] tags)
    {
        var item = new Movie
        {
            InternalId = internalId,
            Name = name,
        };
        item.SetTags(tags);
        item.SetProviderId("imdb", imdbId);
        item.MarkTaggedItemsLoaded();
        return item;
    }

    private static Series CreateSeries(long internalId, string name, string imdbId, params string[] tags)
    {
        var item = new Series
        {
            InternalId = internalId,
            Name = name,
        };
        item.SetTags(tags);
        item.SetProviderId("imdb", imdbId);
        item.MarkTaggedItemsLoaded();
        return item;
    }

    private static BoxSet CreateBoxSet(long internalId, string name)
    {
        var item = new BoxSet
        {
            InternalId = internalId,
            Name = name,
        };
        item.MarkTaggedItemsLoaded();
        return item;
    }

    private static bool MatchesProviderIds(BaseItem item, IEnumerable<KeyValuePair<string, string>> providerIds)
    {
        var lookup = providerIds
            .Select(pair => (pair.Key.ToLowerInvariant(), pair.Value.ToLowerInvariant()))
            .ToHashSet();

        return item.ProviderIds
            .Any(pair => lookup.Contains((pair.Key.ToLowerInvariant(), pair.Value.ToLowerInvariant())));
    }

    private static string Describe(InternalItemsQuery query)
    {
        return
            $"IncludeItemTypes=[{string.Join(",", query.IncludeItemTypes ?? [])}], " +
            $"Tags=[{string.Join(",", query.Tags ?? [])}], " +
            $"ExcludeTags=[{string.Join(",", query.ExcludeTags ?? [])}], " +
            $"CollectionIds=[{string.Join(",", query.CollectionIds ?? [])}], " +
            $"ParentIds=[{string.Join(",", query.ParentIds ?? [])}], " +
            $"AnyProviderIdEquals={query.AnyProviderIdEquals?.Count ?? 0}";
    }
}
