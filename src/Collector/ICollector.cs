using System.Threading;
using System.Threading.Tasks;
using Emby.Plugins.InternetDbCollections.Models.Collection;

namespace Emby.Plugins.InternetDbCollections.Collector;

public interface ICollector
{

    public Task<CollectionItemList> CollectAsync(CancellationToken cancellationToken = default);
}
