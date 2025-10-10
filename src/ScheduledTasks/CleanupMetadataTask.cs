using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugins.InternetDbCollections.Collector;
using Emby.Plugins.InternetDbCollections.Common;
using Emby.Plugins.InternetDbCollections.Utils;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;

namespace Emby.Plugins.InternetDbCollections.ScheduledTasks;

class CleanupMetadataTask(ILibraryManager libraryManager) : IScheduledTask
{
    private readonly ILogger _logger = Plugin.Instance.Logger;

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
            .UseLogger(_logger)
            .Build();
        var metadataManager = new MetadataManager(_logger, libraryManager);
        var observerProgress = new ObserverProgress<double>(progress);
        var tasks =
            collectors
            .Select(collector => BuildCleanupMetadataTask(collector, metadataManager, observerProgress, cancellationToken));
        await Task.WhenAll(tasks);

        _logger.Info("Finish Task {0}", Name);
        progress.Report(100.0);
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return [];
    }

    private async Task BuildCleanupMetadataTask(
        CollectorWithConfig collector,
        MetadataManager metadataManager,
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
            await metadataManager.CleanupMetadataAsync(itemList, observableProgress, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.ErrorException("Error while executing collector `{0}`", ex, collector.ToString());
        }
        finally
        {
            observableProgress.Report(100.0);
        }
    }
}
