namespace Emby.Plugins.InternetDbCollections.Configuration;

using MediaBrowser.Model.Plugins;

public class PluginConfiguration : BasePluginConfiguration
{
    public string MdbListApiKey { get; set; } = string.Empty;

    public CollectorConfiguration[] Collectors { get; set; } =
    {
        new CollectorConfiguration
        {
            Type = CollectorType.ImdbChart,
            Id = "top",
            Name = "IMDb Top 250 movies",
            EnableTags = true,
            EnableCollections = true,
        },
        new CollectorConfiguration
        {
            Type = CollectorType.ImdbChart,
            Id = "toptv",
            Name = "IMDb Top 250 TV shows",
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

    public string Name { get; set; }

    public bool Enabled { get; set; } = true;

    public bool EnableTags { get; set; } = true;

    public bool EnableCollections { get; set; } = true;
}

public static class CollectorType
{
    public const string ImdbChart = "IMDb Chart";
    public const string ImdbList = "IMDb List";
    public const string MdbList = "MDB List";
}
