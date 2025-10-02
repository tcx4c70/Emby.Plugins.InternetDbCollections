using System.Threading;
using System.Threading.Tasks;
using Emby.Plugins.InternetDbCollections.Configuration;
using Emby.Plugins.InternetDbCollections.Models.Collection;

namespace Emby.Plugins.InternetDbCollections.Collector;

public class CollectorWithConfig(ICollector innerCollector, CollectorConfiguration config) : ICollector
{
    public async Task<CollectionItemList> CollectAsync(CancellationToken cancellationToken = default)
    {
        var itemList = await innerCollector.CollectAsync(cancellationToken);
        itemList.EnableTags = Config.EnableTags;
        itemList.EnableCollections = Config.EnableCollections;
        if (!string.IsNullOrWhiteSpace(Config.Name))
        {
            itemList.Name = Config.Name;
        }
        return itemList;
    }

    public CollectorConfiguration Config { get; } = config;

    public override string ToString()
    {
        return $"{{Type: {Config.Type}, Id: {Config.Id}, Name: {Config.Name}, Enabled: {Config.Enabled}, EnableTags: {Config.EnableTags}, EnableCollections: {Config.EnableCollections}}}";
    }
}
