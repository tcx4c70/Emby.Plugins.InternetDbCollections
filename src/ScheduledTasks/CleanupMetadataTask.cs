using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugins.InternetDbCollections.Collector;
using Emby.Plugins.InternetDbCollections.Common;
using Emby.Plugins.InternetDbCollections.Models.Collection;
using Emby.Plugins.InternetDbCollections.Utils;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;

namespace Emby.Plugins.InternetDbCollections.ScheduledTasks;

class CleanupMetadataTask(ILibraryManager libraryManager, ILogManager logManager) : IScheduledTask
{
    private static readonly string s_logName = $"{Plugin.Instance.Name}.{nameof(CleanupMetadataTask)}";

    private readonly ILogger _logger = logManager.GetLogger(s_logName);

    public string Name => "Cleanup Metadata";

    public string Description => "Cleanup metadata";

    public string Key => $"{Plugin.Instance.Name}.CleanupInternetDb";

    public string Category => Plugin.Instance.Name;

    public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
    {
        await Task.Yield();

        _logger.Info("Start task {0}", Name);
        progress.Report(0.0);

        var collectors = new CollectorBuilder()
            .UseConfig(Plugin.Instance.Configuration)
            .UseLogManager(logManager)
            .UseLogPrefix(s_logName)
            .Build();
        var observerProgress = new ObserverProgress<double>(new ProgressWithBound(progress, 0, 50));
        var tasks =
            collectors
            .Select(collector => BuildCleanupMetadataTask(collector, observerProgress, cancellationToken));
        var itemLists =
            (await Task.WhenAll(tasks))
            .OfType<CollectionItemList>()
            .ToList();

        var metadataManager = new MetadataManager(_logger, libraryManager);
        var step = itemLists.Count == 0 ? 50.0 : 50.0 / itemLists.Count;
        var currentProgress = 50.0;
        foreach (var itemList in itemLists)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await metadataManager.CleanupMetadataAsync(itemList, new ProgressWithBound(progress, currentProgress, currentProgress + step), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error while cleaning up metadata for item list {0}", ex, itemList.Name);
            }

            currentProgress += step;
        }

        _logger.Info("Finish Task {0}", Name);
        progress.Report(100.0);
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return [];
    }

    private async Task<CollectionItemList?> BuildCleanupMetadataTask(
        CollectorWithConfig collector,
        ObserverProgress<double> progress,
        CancellationToken cancellationToken = default)
    {
        var observableProgress = new ObservableProgress<double>();
        observableProgress.ProgressChanged += progress.Report;
        progress.AddObservable(observableProgress);

        // Make it fully asynchronous
        await Task.Yield();
        try
        {
            var itemList = await collector.CollectAsync(cancellationToken);
            return itemList;
        }
        catch (Exception ex)
        {
            _logger.ErrorException("Error while executing collector `{0}`", ex, collector.ToString());
            return null;
        }
        finally
        {
            observableProgress.Report(100.0);
        }
    }
}
