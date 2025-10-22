using System;
using System.Collections.Generic;
using System.Linq;
using Emby.Plugins.InternetDbCollections.Configuration;
using Emby.Plugins.InternetDbCollections.Models.Collection;
using MediaBrowser.Model.Logging;

namespace Emby.Plugins.InternetDbCollections.Collector;

class CollectorBuilder
{
    private PluginConfiguration? _config;
    private ILogManager? _logManager;
    private string _logPrefix = Plugin.Instance.Name;
    private bool _enableCron = false;

    public PluginConfiguration Config => _config ?? throw new InvalidOperationException("Configuration has not been set");

    public ILogManager LogManager => _logManager ?? throw new InvalidOperationException("LogManager has not been set");

    public CollectorBuilder UseConfig(PluginConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config), "Configuration cannot be null");
        return this;
    }

    public CollectorBuilder UseLogManager(ILogManager logManager)
    {
        _logManager = logManager ?? throw new ArgumentNullException(nameof(logManager), "LogManager cannot be null");
        return this;
    }

    public CollectorBuilder UseLogPrefix(string logPrefix)
    {
        _logPrefix = logPrefix ?? throw new ArgumentNullException(nameof(logPrefix), "Log prefix cannot be null");
        return this;
    }

    public CollectorBuilder EnableCron()
    {
        _enableCron = true;
        return this;
    }

    public List<CollectorWithConfig> Build()
    {
        if (_config == null)
        {
            throw new InvalidOperationException("Configuration must be set before building collectors");
        }
        if (_logManager == null)
        {
            throw new InvalidOperationException("LogManager must be set before building collectors");
        }

        return _config.Collectors
            .Select(BuildOne)
            .OfType<CollectorWithConfig>()
            .ToList();
    }

    private CollectorWithConfig? BuildOne(CollectorConfiguration collectorConfig)
    {
        if (!collectorConfig.ShouldCollectNow(_enableCron))
        {
            return null;
        }

        ICollector collector;
        string logName = $"{_logPrefix}.{collectorConfig.Type.ToProviderName()}.{collectorConfig.Id}";
        var logger = LogManager.GetLogger(logName);
        switch (collectorConfig.Type)
        {
            case CollectorType.ImdbChart:
                collector = new ImdbChartCollector(collectorConfig.Id, logger);
                break;
            case CollectorType.ImdbList:
                collector = new ImdbListCollector(collectorConfig.Id, logger);
                break;
            case CollectorType.TraktList:
                collector = new TraktListCollector(collectorConfig.Id, Config.TraktClientId, logger);
                break;
            case CollectorType.MdbList:
                collector = new MdbListCollector(collectorConfig.Id, Config.MdbListApiKey, logger);
                break;
            case CollectorType.Letterboxd:
                collector = new LetterboxdCollector(collectorConfig.Id, logger);
                break;
            default:
                return null;
        }

        return new CollectorWithConfig(collector, collectorConfig);
    }
}
