namespace Emby.Plugins.InternetDbCollections.ScheduledTasks;

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

    public CollectInternetDbTask(ILogManager logManager, ILibraryManager libraryManager)
    {
        _logger = logManager.GetLogger(nameof(CollectInternetDbTask));
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

        var imdbTop250 = new ImdbTop250(_logger, _libraryManager);
        await imdbTop250.InitializeAsync(cancellationToken);
        await imdbTop250.UpdateTagsAsync(progress, cancellationToken);

        _logger.Info("Finish Task {0}", Name);
        progress?.Report(100.0);
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfo.TriggerWeekly,
            TimeOfDayTicks = TimeSpan.FromHours(3).Ticks,
        };
    }
}
