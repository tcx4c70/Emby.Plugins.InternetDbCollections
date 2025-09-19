using System.Collections.Generic;

namespace Emby.Plugins.InternetDbCollections.Models.Collection;

public class CollectionItemList
{
    public string Name { get; set; }

    public string? Description { get; set; } = null;

    public bool EnableTags { get; set; }

    public bool EnableCollections { get; set; }

    public IDictionary<string, string> Ids = new Dictionary<string, string>();

    public IEnumerable<CollectionItem> Items { get; set; } = new List<CollectionItem>();
}
