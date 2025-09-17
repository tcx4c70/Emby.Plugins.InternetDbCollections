namespace Emby.Plugins.InternetDbCollections.Models.Trakt;

using System.Text.Json.Serialization;

public class TraktPersonItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("ids")]
    public TraktItemIds Ids { get; set; }
}
