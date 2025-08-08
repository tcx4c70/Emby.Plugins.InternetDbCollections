namespace Emby.Plugins.InternetDbCollections.Configuration;

public class CollectorConfiguration
{
    public string Type { get; set; }

    // TODO: Do all types need an ID?
    public string Id { get; set; }

    public string Name { get; set; }

    public bool Enabled { get; set; } = true;

    public bool EnableTags { get; set; } = true;

    public bool EnableCollections { get; set; } = true;
}
