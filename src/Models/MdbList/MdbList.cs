using System.Text.Json.Serialization;

namespace Emby.Plugins.InternetDbCollections.Models.MdbList;

public class MdbList
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("user_name")]
    public required string UserName { get; set; }

    [JsonPropertyName("slug")]
    public required string Slug { get; set; }
}
