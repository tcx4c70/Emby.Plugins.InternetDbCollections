using System.Threading;
using System.Threading.Tasks;
using Emby.Plugins.InternetDbCollections.Configuration;

namespace Emby.Plugins.InternetDbCollections.Collector;

public class CollectorWithConfig : ICollector
{
    private readonly ICollector _innerCollector;
    private readonly CollectorConfiguration _config;

    public CollectorWithConfig(ICollector innerCollector, CollectorConfiguration config)
    {
        _innerCollector = innerCollector;
        _config = config;
    }

    public async Task<CollectionItemList> CollectAsync(CancellationToken cancellationToken = default)
    {
        var itemList = await _innerCollector.CollectAsync(cancellationToken);
        itemList.EnableTags = _config.EnableTags;
        itemList.EnableCollections = _config.EnableCollections;
        if (!string.IsNullOrWhiteSpace(_config.Name))
        {
            itemList.Name = _config.Name;
        }
        return itemList;
    }

    public override string ToString()
    {
        return $"{{Type: {_config.Type}, Id: {_config.Id}, Name: {_config.Name}, Enabled: {_config.Enabled}, EnableTags: {_config.EnableTags}, EnableCollections: {_config.EnableCollections}}}";
    }
}
