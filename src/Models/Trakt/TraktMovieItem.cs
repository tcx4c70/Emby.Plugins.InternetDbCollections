using System.Text.Json.Serialization;

namespace Emby.Plugins.InternetDbCollections.Models.Trakt;

public class TraktMovieItem
{
    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("ids")]
    public TraktItemIds Ids { get; set; } = new();
}
