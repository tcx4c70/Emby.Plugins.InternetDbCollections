using System.Text.Json.Serialization;

namespace Emby.Plugins.InternetDbCollections.Models.Trakt;

public class TraktMovieItem
{
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("ids")]
    public TraktItemIds Ids { get; set; }
}
