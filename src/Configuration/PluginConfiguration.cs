namespace Emby.Plugins.InternetDbCollections.Configuration;

using MediaBrowser.Model.Plugins;

public class PluginConfiguration : BasePluginConfiguration
{
    public CollectorConfiguration[] Collectors { get; set; } =
    {
        new CollectorConfiguration
        {
            Type = CollectorType.ImdbChart,
            Id = "top",
            EnableTags = true,
            EnableCollections = true,
        },
        new CollectorConfiguration
        {
            Type = CollectorType.ImdbChart,
            Id = "toptv",
            EnableTags = true,
            EnableCollections = true,
        },
        new CollectorConfiguration
        {
            Type = CollectorType.ImdbList,
            Id = "ls055592025",
            EnableTags = true,
            EnableCollections = false,
        },
    };
}

public class CollectorConfiguration
{
    public string Type { get; set; }

    // TODO: Do all types need an ID?
    public string Id { get; set; }

    public bool Enabled { get; set; } = true;

    public bool EnableTags { get; set; } = true;

    public bool EnableCollections { get; set; } = true;
}

public static class CollectorType
{
    public const string ImdbChart = "IMDb Chart";
    public const string ImdbList = "IMDb List";
}
