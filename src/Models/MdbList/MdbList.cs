namespace Emby.Plugins.InternetDbCollections.Models.MdbList;

using System.Text.Json.Serialization;

public class MdbList
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("user_name")]
    public string UserName { get; set; }

    [JsonPropertyName("slug")]
    public string Slug { get; set; }
}
