using System.Text.Json.Serialization;

namespace Emby.Plugins.InternetDbCollections.Models.Trakt;

public class TraktList
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("ids")]
    public TraktListIds Ids { get; set; } = new();
}

public class TraktListIds
{
    [JsonPropertyName("trakt")]
    public int Trakt { get; set; }
}
