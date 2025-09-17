namespace Emby.Plugins.InternetDbCollections.Models.MdbList;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public class MdbListListItemsResponse
{
    [JsonPropertyName("movies")]
    public List<MdbListItem> Movies { get; set; } = new();

    [JsonPropertyName("shows")]
    public List<MdbListItem> Shows { get; set; } = new();
}
