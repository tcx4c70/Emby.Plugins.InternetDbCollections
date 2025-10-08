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

            foreach (var item in listPage.DocumentNode.SelectNodes("//li[@class='posteritem numbered-list-item']"))
            {
                var reactNode = item.SelectSingleNode(".//div[@class='react-component']");
                var id = reactNode.Attributes["data-film-id"].Value;
                var slug = reactNode.Attributes["data-item-slug"].Value;
                var rank = item.SelectSingleNode("./p[@class='list-number']").InnerText.Trim();

                // TODO: It's very slow to fetch each movie detail page to get IMDb ID. Try to fetch all movie detail pages in parallel. Will it be blocked/throttled by Letterboxd?
                var movieDoc = await web.LoadFromWebAsync($"{s_baseUrl}/film/{slug}/", cancellationToken);
                var imdbLink = movieDoc.DocumentNode.SelectSingleNode("//a[string()='IMDb']");
                var imdbId = imdbLink.Attributes["href"].Value.Split('/')[4];

                items.Add(new CollectionItem()
                {
                    Order = int.Parse(rank),
                    // TODO: Letterboxd supports TV shows too, but all TV shows are with type film. e.g. https://letterboxd.com/slinkyman/list/letterboxds-top-100-highest-rated-documentary/
                    // How can we detect them?
                    Type = nameof(Movie),
                    Ids = new Dictionary<string, string>()
                    {
                        { "imdb", imdbId },
                        { "letterboxd", id },
                    }
                });
            }

            logger.Info("Parsed Letterboxd list '{0}' page {1}, found {2} items", listId, page, items.Count);

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
