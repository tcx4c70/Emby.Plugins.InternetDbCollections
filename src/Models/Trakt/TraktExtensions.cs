using System;
using System.Collections.Generic;
using Emby.Plugins.InternetDbCollections.Collector;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;

namespace Emby.Plugins.InternetDbCollections.Models.Trakt;

public static class TraktExtensions
{
    public static string ToEmbyItemType(this TraktItemType itemType) => itemType switch
    {
        TraktItemType.Movie => nameof(Movie),
        TraktItemType.Show => nameof(Series),
        TraktItemType.Season => nameof(Season),
        TraktItemType.Episode => nameof(Episode),
        _ => throw new ArgumentOutOfRangeException(nameof(itemType), $"Not expected Trakt item type value: {itemType}"),
    };

    public static IDictionary<string, string> ToCollectionItemIds(this TraktItemIds ids)
    {
        var dict = new Dictionary<string, string>();
        if (ids.Imdb != null)
        {
            dict.Add("imdb", ids.Imdb);
        }
        if (ids.Tmdb != null)
        {
            dict.Add("tmdb", ids.Tmdb.Value.ToString());
        }
        if (ids.Tvdb != null)
        {
            dict.Add("tvdb", ids.Tvdb.Value.ToString());
        }
        return dict;
    }

    public static CollectionItem ToCollectionItem(this TraktItem item) => new()
    {
        Order = item.Rank,
        Type = item.Type.ToEmbyItemType(),
        Ids = item.Type switch
        {
            TraktItemType.Movie => item.Movie.Ids.ToCollectionItemIds(),
            TraktItemType.Show => item.Show.Ids.ToCollectionItemIds(),
            _ => throw new NotSupportedException($"Trakt item type '{item.Type}' is not supported"),
        }
    };
}
