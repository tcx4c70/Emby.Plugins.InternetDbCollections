namespace Emby.Plugins.InternetDbCollections.Models.Trakt;

using System.Text.Json.Serialization;

public class TraktList
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("ids")]
    public TraktListIds Ids { get; set; }
}

public class TraktListIds
{
    [JsonPropertyName("trakt")]
    public int Trakt { get; set; }
}
