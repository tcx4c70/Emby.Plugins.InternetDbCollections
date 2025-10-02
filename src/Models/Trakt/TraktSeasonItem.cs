using System.Text.Json.Serialization;

namespace Emby.Plugins.InternetDbCollections.Models.Trakt;

public class TraktSeasonItem
{
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("ids")]
    public TraktItemIds Ids { get; set; } = new();
}
