using System.Text.Json.Serialization;

namespace Emby.Plugins.InternetDbCollections.Models.Trakt;

public class TraktItemIds
{
    [JsonPropertyName("trakt")]
    public int? Trakt { get; set; }

    [JsonPropertyName("imdb")]
    public string? Imdb { get; set; }

    [JsonPropertyName("tvdb")]
    public int? Tvdb { get; set; }

    [JsonPropertyName("tmdb")]
    public int? Tmdb { get; set; }
}
