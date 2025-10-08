using System;

namespace Emby.Plugins.InternetDbCollections.Models.Collection;

public static class CollectorType
{
    public const string ImdbChart = "IMDb Chart";
    public const string ImdbList = "IMDb List";
    public const string TraktList = "Trakt List";
    public const string MdbList = "MDB List";
    public const string Letterboxd = "Letterboxd";

    public static string ToProviderName(this string collectorType)
    {
        return collectorType switch
        {
            ImdbChart => "IMDbChart",
            ImdbList => "IMDbList",
            TraktList => "TraktList",
            MdbList => "MDBList",
            Letterboxd => "Letterboxd",
            _ => throw new ArgumentException($"Unknown collector type: {collectorType}"),
        };
    }
}
