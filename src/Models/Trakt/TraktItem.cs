using System.Text.Json.Serialization;

namespace Emby.Plugins.InternetDbCollections.Models.Trakt;

public class TraktItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("rank")]
    public int Rank { get; set; }

    [JsonPropertyName("type")]
    public TraktItemType Type { get; set; }

    [JsonPropertyName("movie")]
    public TraktMovieItem? Movie { get; set; }

    [JsonPropertyName("show")]
    public TraktShowItem? Show { get; set; }

    [JsonPropertyName("season")]
    public TraktSeasonItem? Season { get; set; }

    [JsonPropertyName("episode")]
    public TraktEpisodeItem? Episode { get; set; }

    [JsonPropertyName("person")]
    public TraktPersonItem? Person { get; set; }
}
