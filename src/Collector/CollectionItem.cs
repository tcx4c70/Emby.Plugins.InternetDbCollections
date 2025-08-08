namespace Emby.Plugins.InternetDbCollections.Collector;

using System.Collections.Generic;

public class CollectionItem
{
    public int Order { get; init; }
    public IDictionary<string, string> Ids { get; init; } = new Dictionary<string, string>();
    public string Type { get; init; }
}
