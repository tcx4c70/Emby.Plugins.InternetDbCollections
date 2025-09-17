namespace Emby.Plugins.InternetDbCollections.Models.Trakt;

using System.Text.Json.Serialization;

public class TraktSeasonItem
{
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("ids")]
    public TraktItemIds Ids { get; set; }
}
