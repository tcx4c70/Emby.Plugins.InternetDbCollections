using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Emby.Plugins.InternetDbCollections.Models.MdbList;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MdbListMediaType
{
    [EnumMember(Value = "movie")]
    Movie,
    [EnumMember(Value = "show")]
    Show,
}
