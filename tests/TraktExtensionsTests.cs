using Emby.Plugins.InternetDbCollections.Models.Trakt;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;

namespace Emby.Plugins.InternetDbCollections.Tests;

public class TraktExtensionsTests
{
    [Fact]
    public void ToCollectionItem_maps_movie_items()
    {
        var item = new TraktItem
        {
            Rank = 7,
            Type = TraktItemType.Movie,
            Movie = new TraktMovieItem
            {
                Title = "Movie",
                Ids = new TraktItemIds
                {
                    Imdb = "tt1234567",
                    Tmdb = 42,
                    Tvdb = 84,
                },
            },
        };

        var result = item.ToCollectionItem();

        Assert.Equal(7, result.Order);
        Assert.Equal(nameof(Movie), result.Type);
        Assert.Equal("tt1234567", result.Ids["imdb"]);
        Assert.Equal("42", result.Ids["tmdb"]);
        Assert.Equal("84", result.Ids["tvdb"]);
    }

    [Fact]
    public void ToCollectionItem_maps_show_items()
    {
        var item = new TraktItem
        {
            Rank = 1,
            Type = TraktItemType.Show,
            Show = new TraktShowItem
            {
                Title = "Show",
                Ids = new TraktItemIds
                {
                    Imdb = "tt7654321",
                    Tvdb = 123,
                },
            },
        };

        var result = item.ToCollectionItem();

        Assert.Equal(1, result.Order);
        Assert.Equal(nameof(Series), result.Type);
        Assert.Equal("tt7654321", result.Ids["imdb"]);
        Assert.Equal("123", result.Ids["tvdb"]);
        Assert.False(result.Ids.ContainsKey("tmdb"));
    }
}
