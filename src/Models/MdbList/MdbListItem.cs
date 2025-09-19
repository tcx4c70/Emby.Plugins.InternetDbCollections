using System.Text.Json.Serialization;

namespace Emby.Plugins.InternetDbCollections.Models.MdbList;

public class MdbListItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("rank")]
    public int Rank { get; set; }

    [JsonPropertyName("mediatype")]
    public MdbListMediaType MediaType { get; set; }

    [JsonPropertyName("imdb_id")]
    public string ImdbId { get; set; }

    [JsonPropertyName("tvdb_id")]
    public int? TvdbId { get; set; }
}
