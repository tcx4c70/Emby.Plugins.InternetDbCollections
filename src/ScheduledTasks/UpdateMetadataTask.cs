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

class UpdateMetadataTask(ILibraryManager libraryManager, ILogManager logManager) : IScheduledTask
{
    private static readonly string s_logName = $"{Plugin.Instance.Name}.{nameof(UpdateMetadataTask)}";

    private readonly ILogger _logger = logManager.GetLogger(s_logName);

    public string Name => "Update Metadata";

    public string Description => "Collect metadata from various internet DBs and updates the library";

    public string Key => $"{Plugin.Instance.Name}.CollectInternetDb";

    public string Category => Plugin.Instance.Name;

    public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
    {
        await Task.Yield();

        var now = DateTime.UtcNow;
        _logger.Info("Start task {0}", Name);
        progress.Report(0.0);

        var collectors = new CollectorBuilder()
            .UseConfig(Plugin.Instance.Configuration)
            .UseLogManager(logManager)
            .UseLogPrefix(s_logName)
            .EnableCron()
            .Build();
        var observerProgress = new ObserverProgress<double>(new ProgressWithBound(progress, 0, 50));
        var tasks =
            collectors
            .Select(collector => BuildUpdateMetadataTask(collector, now, observerProgress, cancellationToken));
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
                await metadataManager.UpdateMetadataAsync(itemList, new ProgressWithBound(progress, currentProgress, currentProgress + step), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error while updating metadata for item list {0}", ex, itemList.Name);
            }

            currentProgress += step;
        }

        Plugin.Instance.SaveConfiguration();
        _logger.Info("Finish Task {0}", Name);
        progress.Report(100.0);
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfo.TriggerInterval,
            IntervalTicks = TimeSpan.FromHours(1).Ticks,
        };
    }

    private async Task<CollectionItemList?> BuildUpdateMetadataTask(
        CollectorWithConfig collector,
        DateTime startTime,
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
            collector.Config.LastCollected = startTime;
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
