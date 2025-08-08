namespace Emby.Plugins.InternetDbCollections.Collector;

using System.Collections.Generic;

public class CollectionItemList
{
    public string Name { get; set; }

    public string? Description { get; set; } = null;

    public bool EnableTags { get; set; }

    public bool EnableCollections { get; set; }

    public IEnumerable<CollectionItem> Items { get; set; } = new List<CollectionItem>();
}
