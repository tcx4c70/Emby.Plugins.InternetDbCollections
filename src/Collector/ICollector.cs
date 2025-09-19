using System.Threading;
using System.Threading.Tasks;

namespace Emby.Plugins.InternetDbCollections.Collector;

public interface ICollector
{

    public Task<CollectionItemList> CollectAsync(CancellationToken cancellationToken = default);
}
