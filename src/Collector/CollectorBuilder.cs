namespace Emby.Plugins.InternetDbCollections.Collector;

using System;
using System.Collections.Generic;
using System.Linq;
using Emby.Plugins.InternetDbCollections.Configuration;
using MediaBrowser.Model.Logging;

class CollectorBuilder
{
    private IEnumerable<CollectorConfiguration> _configs = new List<CollectorConfiguration>();
    private ILogger _logger;

    public CollectorBuilder UseConfigs(IEnumerable<CollectorConfiguration> configs)
    {
        _configs = configs;
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

        return _configs
            .Select(BuildOne)
            .Where(collector => collector != null)
            .ToList();
    }

    private ICollector BuildOne(CollectorConfiguration config)
    {
        if (!config.Enabled)
        {
            return null;
        }

        switch (config.Type)
        {
            case CollectorType.ImdbChart:
                return new CollectorWithConfig(new ImdbChartCollector(config.Id, _logger), config);
            case CollectorType.ImdbList:
                return new CollectorWithConfig(new ImdbListCollector(config.Id, _logger), config);
            default:
                _logger.Warn("Unknown collector type `{0}`, skip", config.Type);
                return null;
        }
    }
}
