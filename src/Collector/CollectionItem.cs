using System.Collections.Generic;

namespace Emby.Plugins.InternetDbCollections.Collector;

public class CollectionItem
{
    public int Order { get; init; }
    public IDictionary<string, string> Ids { get; init; } = new Dictionary<string, string>();
    public string Type { get; init; }
}
