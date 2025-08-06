namespace Emby.Plugins.InternetDbCollections.Collector;

using System;
using System.Threading;
using System.Threading.Tasks;

public interface ICollector
{

    public Task<CollectionItemList> CollectAsync(CancellationToken cancellationToken = default);
}
