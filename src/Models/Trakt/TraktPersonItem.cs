using System.Text.Json.Serialization;

namespace Emby.Plugins.InternetDbCollections.Models.Trakt;

public class TraktPersonItem
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("ids")]
    public TraktItemIds Ids { get; set; } = new();
}
