namespace Emby.Plugins.InternetDbCollections.ScheduledTasks;

using Emby.Plugins.InternetDbCollections.Collector;
using Emby.Plugins.InternetDbCollections.Common;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class CollectInternetDbTask : IScheduledTask
{
    private readonly ILogger _logger;
    private readonly ILibraryManager _libraryManager;

    public CollectInternetDbTask(ILibraryManager libraryManager)
    {
        _logger = Plugin.Instance.Logger;
        _libraryManager = libraryManager;
    }

    public string Name => "Update Metadata";

    public string Description => "Collect metadata from various internet DBs and updates the library";

    public string Key => $"{Plugin.Instance.Name}.CollectInternetDb";

    public string Category => Plugin.Instance.Name;

    public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
    {
        await Task.Yield();

        _logger.Info("Start task {0}", Name);
        progress?.Report(0.0);

        List<ICollector> collectors = new()
        {
            new ImdbChartCollector("top", _logger, _libraryManager),
            new ImdbChartCollector("toptv", _logger, _libraryManager),
        };

        double step = 100.0 / collectors.Count;
        double currentProgress = 0.0;
        foreach (var collector in collectors)
        {
            await collector.InitializeAsync(cancellationToken);
            await collector.UpdateMetadataAsync(new ProgressWithBound(progress, currentProgress, currentProgress + step), cancellationToken);
            currentProgress += step;
        }

        _logger.Info("Finish Task {0}", Name);
        progress?.Report(100.0);
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfo.TriggerWeekly,
            DayOfWeek = DayOfWeek.Sunday,
            TimeOfDayTicks = TimeSpan.FromHours(3).Ticks,
        };
    }
}
