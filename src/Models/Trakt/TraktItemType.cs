using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Emby.Plugins.InternetDbCollections.Models.Trakt;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TraktItemType
{
    [EnumMember(Value = "movie")]
    Movie,
    [EnumMember(Value = "show")]
    Show,
    [EnumMember(Value = "season")]
    Season,
    [EnumMember(Value = "episode")]
    Episode,
    [EnumMember(Value = "person")]
    Person,
}
