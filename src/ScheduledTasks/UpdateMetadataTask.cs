using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugins.InternetDbCollections.Collector;
using Emby.Plugins.InternetDbCollections.Common;
using Emby.Plugins.InternetDbCollections.Utils;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;

namespace Emby.Plugins.InternetDbCollections.ScheduledTasks;

class UpdateMetadataTask(ILibraryManager libraryManager) : IScheduledTask
{
    private readonly ILogger _logger = Plugin.Instance.Logger;

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
            .UseLogger(_logger)
            .EnableCron()
            .Build();
        var metadataManager = new MetadataManager(_logger, libraryManager);
        double step = collectors.Count == 0 ? 100.0 : 100.0 / collectors.Count;
        double currentProgress = 0.0;
        foreach (var collector in collectors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var itemList = await collector.CollectAsync(cancellationToken);
                await metadataManager.UpdateMetadataAsync(itemList, new ProgressWithBound(progress, currentProgress, currentProgress + step), cancellationToken);
                collector.Config.LastCollected = now;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error while executing collector `{0}`", ex, collector.ToString());
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
}
