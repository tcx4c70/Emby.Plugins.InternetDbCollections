using System.Text.Json.Serialization;

namespace Emby.Plugins.InternetDbCollections.Models.Trakt;

public class TraktEpisodeItem
{
    [JsonPropertyName("season")]
    public int Season { get; set; }

    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("ids")]
    public TraktItemIds Ids { get; set; }
}
