using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugins.InternetDbCollections.Models.Collection;
using HtmlAgilityPack;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Logging;

namespace Emby.Plugins.InternetDbCollections.Collector;

class LetterboxdCollector(string listId, ILogger logger) : ICollector
{
    private static readonly string s_baseUrl = "https://letterboxd.com";

    public async Task<CollectionItemList> CollectAsync(CancellationToken cancellationToken = default)
    {
        var web = new HtmlWeb();
        var page = 1;
        string? name = null;
        string? description = null;
        var items = new List<CollectionItem>();

        while (true)
        {
            var url = $"{s_baseUrl}/{listId}/page/{page}/";
            logger.Debug("Fetching Letterboxd list '{0}' page {1} data from {2}", listId, page, url);
            var listPage = await web.LoadFromWebAsync($"{s_baseUrl}/{listId}/page/{page}/", cancellationToken);
            logger.Debug("Received Letterboxd list '{0}' page {1} data, parsing...", listId, page);

            name ??= listPage.DocumentNode.SelectSingleNode("//h1[@class='title-1 prettify']").InnerText;
            description ??= listPage.DocumentNode
                .SelectNodes("//div[@id='list-notes']/p")
                .Select(p => p.InnerText)
                .Aggregate((a, b) => a + "\n" + b);

            var posterItems = listPage.DocumentNode.SelectNodes("//li[@class='posteritem numbered-list-item']");
            var imdbIds = await Task.WhenAll(
                posterItems
                .Select(item => item.SelectSingleNode(".//div[@class='react-component']"))
                .Select(node => node.Attributes["data-item-slug"].Value)
                .Select(async slug =>
                {
                    var movieDetail = await web.LoadFromWebAsync($"{s_baseUrl}/film/{slug}/", cancellationToken);
                    var imdbLink = movieDetail.DocumentNode.SelectSingleNode("//a[string()='IMDb']");
                    var imdbId = imdbLink.Attributes["href"].Value.Split('/')[4];
                    return imdbId;
                }));
            var pageItems = posterItems
                .Select(posterItem => posterItem.SelectSingleNode("./p[@class='list-number']").InnerText.Trim())
                .Zip(imdbIds, (rank, imdbId) => new CollectionItem()
                {
                    Order = int.Parse(rank),
                    Type = nameof(Movie),
                    Ids = new Dictionary<string, string>()
                    {
                        { "imdb", imdbId },
                    },
                })
                .ToList();
            items.AddRange(pageItems);
            logger.Info("Parsed Letterboxd list '{0}' page {1}, found {2} items", listId, page, pageItems.Count);

            if (listPage.DocumentNode.SelectSingleNode("//a[@class='next']") == null)
            {
                break;
            }
            page++;
        }

        logger.Info("Completed parsing Letterboxd list '{0}', total {1} items", listId, items.Count);
        return new CollectionItemList()
        {
            Name = name ?? listId,
            Description = description ?? string.Empty,
            Ids =
            {
                { CollectorType.Letterboxd.ToProviderName(), Uri.EscapeDataString(listId) },
            },
            Items = items,
        };
    }
}
