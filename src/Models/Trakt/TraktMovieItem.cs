namespace Emby.Plugins.InternetDbCollections.Models.Trakt;

using System.Text.Json.Serialization;

public class TraktMovieItem
{
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("ids")]
    public TraktItemIds Ids { get; set; }
}
