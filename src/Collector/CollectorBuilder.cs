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
    private ILogger? _logger;
    private bool _enableCron = false;

    public PluginConfiguration Config => _config ?? throw new InvalidOperationException("Configuration has not been set");

    public ILogger Logger => _logger ?? throw new InvalidOperationException("Logger has not been set");

    public CollectorBuilder UseConfig(PluginConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config), "Configuration cannot be null");
        return this;
    }

    public CollectorBuilder UseLogger(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null");
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
        if (_logger == null)
        {
            throw new InvalidOperationException("Logger must be set before building collectors");
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
        switch (collectorConfig.Type)
        {
            case CollectorType.ImdbChart:
                collector = new ImdbChartCollector(collectorConfig.Id, Logger);
                break;
            case CollectorType.ImdbList:
                collector = new ImdbListCollector(collectorConfig.Id, Logger);
                break;
            case CollectorType.TraktList:
                collector = new TraktListCollector(collectorConfig.Id, Config.TraktClientId, Logger);
                break;
            case CollectorType.MdbList:
                collector = new MdbListCollector(collectorConfig.Id, Config.MdbListApiKey, Logger);
                break;
            default:
                Logger.Warn("Unknown collector type `{0}`, skip", collectorConfig.Type);
                return null;
        }

        return new CollectorWithConfig(collector, collectorConfig);
    }
}
