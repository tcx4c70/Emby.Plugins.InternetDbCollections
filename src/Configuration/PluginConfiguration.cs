namespace Emby.Plugins.InternetDbCollections.Configuration;

using Emby.Plugins.InternetDbCollections.Common;
using MediaBrowser.Model.Plugins;

public class PluginConfiguration : BasePluginConfiguration
{
    public string MdbListApiKey { get; set; } = string.Empty;

    public string TraktClientId { get; set; } = string.Empty;

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
