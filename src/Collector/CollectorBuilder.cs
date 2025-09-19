using System;
using System.Collections.Generic;
using System.Linq;
using Emby.Plugins.InternetDbCollections.Common;
using Emby.Plugins.InternetDbCollections.Configuration;
using MediaBrowser.Model.Logging;

namespace Emby.Plugins.InternetDbCollections.Collector;

class CollectorBuilder
{
    private PluginConfiguration _config;
    private ILogger _logger;

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

    public List<ICollector> Build()
    {
        if (_logger == null)
        {
            throw new InvalidOperationException("Logger must be set before building collectors");
        }

        return _config.Collectors
            .Select(BuildOne)
            .Where(collector => collector != null)
            .ToList();
    }

    private ICollector BuildOne(CollectorConfiguration collectorConfig)
    {
        if (!collectorConfig.Enabled)
        {
            return null;
        }

        switch (collectorConfig.Type)
        {
            case CollectorType.ImdbChart:
                return new CollectorWithConfig(new ImdbChartCollector(collectorConfig.Id, _logger), collectorConfig);
            case CollectorType.ImdbList:
                return new CollectorWithConfig(new ImdbListCollector(collectorConfig.Id, _logger), collectorConfig);
            case CollectorType.TraktList:
                return new CollectorWithConfig(new TraktListCollector(collectorConfig.Id, _config.TraktClientId, _logger), collectorConfig);
            case CollectorType.MdbList:
                return new CollectorWithConfig(new MdbListCollector(collectorConfig.Id, _config.MdbListApiKey, _logger), collectorConfig);
            default:
                _logger.Warn("Unknown collector type `{0}`, skip", collectorConfig.Type);
                return null;
        }
    }
}
