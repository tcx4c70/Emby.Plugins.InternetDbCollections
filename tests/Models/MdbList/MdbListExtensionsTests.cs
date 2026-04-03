using Emby.Plugins.InternetDbCollections.Models.MdbList;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;

namespace Emby.Plugins.InternetDbCollections.Tests;

public class MdbListExtensionsTests
{
    [Fact]
    public void ToCollectionItem_maps_movie_items()
    {
        var item = new MdbListItem
        {
            Rank = 4,
            MediaType = MdbListMediaType.Movie,
            ImdbId = "tt1111111",
            TvdbId = 222,
        };

        var result = item.ToCollectionItem();

        Assert.Equal(4, result.Order);
        Assert.Equal(nameof(Movie), result.Type);
        Assert.Equal("tt1111111", result.Ids["imdb"]);
        Assert.Equal("222", result.Ids["tvdb"]);
    }

    [Fact]
    public void ToCollectionItem_maps_show_items_without_optional_tvdb()
    {
        var item = new MdbListItem
        {
            Rank = 9,
            MediaType = MdbListMediaType.Show,
            ImdbId = "tt2222222",
            TvdbId = null,
        };

        var result = item.ToCollectionItem();

        Assert.Equal(9, result.Order);
        Assert.Equal(nameof(Series), result.Type);
        Assert.Equal("tt2222222", result.Ids["imdb"]);
        Assert.False(result.Ids.ContainsKey("tvdb"));
    }
}
