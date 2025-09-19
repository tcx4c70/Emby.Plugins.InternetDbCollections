using System;
using System.Collections.Generic;
using Emby.Plugins.InternetDbCollections.Collector;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;

namespace Emby.Plugins.InternetDbCollections.Models.MdbList;

public static class MdbListExtensions
{
    public static string ToEmbyItemType(this MdbListMediaType mediaType) => mediaType switch
    {
        MdbListMediaType.Movie => nameof(Movie),
        MdbListMediaType.Show => nameof(Series),
        _ => throw new ArgumentOutOfRangeException(nameof(mediaType), $"Not expected MdbList media type: {mediaType}"),
    };

    public static CollectionItem ToCollectionItem(this MdbListItem item)
    {
        Dictionary<string, string> ids = new()
        {
            { "imdb", item.ImdbId },
        };
        if (item.TvdbId is not null)
        {
            ids["tvdb"] = item.TvdbId.ToString();
        }

        return new CollectionItem
        {
            Order = item.Rank,
            Ids = ids,
            Type = item.MediaType.ToEmbyItemType(),
        };
    }
}
