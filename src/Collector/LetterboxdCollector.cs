using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugins.InternetDbCollections.Models.Collection;
using Emby.Plugins.InternetDbCollections.Utils;
using HtmlAgilityPack;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Logging;

namespace Emby.Plugins.InternetDbCollections.Collector;

class LetterboxdCollector(string listId, ILogger logger) : ICollector
{
    private static readonly string s_baseUrl = "https://letterboxd.com";
    private static readonly string s_imdbBeginTag = "www.imdb.com/title/";

    public async Task<CollectionItemList> CollectAsync(CancellationToken cancellationToken = default)
    {
        var httpClient = HttpClientPool.Instance.GetClient("letterboxd", client => client.BaseAddress = new Uri("https://letterboxd.com"));
        var page = 1;
        string? name = null;
        string? description = null;
        var ranks = new List<int>();
        var letterboxdIds = new List<string>();
        var imdbIdTasks = new List<Task<string>>();

        while (true)
        {
            var url = $"{s_baseUrl}/{listId}/page/{page}/";
            logger.Debug("Fetching Letterboxd list '{0}' page {1} data from {2}", listId, page, url);
            var htmlContent = await httpClient.GetStringAsync($"/{listId}/page/{page}/", cancellationToken: cancellationToken);
            var listPage = new HtmlDocument();
            listPage.LoadHtml(htmlContent);
            logger.Debug("Received Letterboxd list '{0}' page {1} data, parsing...", listId, page);

            name ??= ParseName(listPage);
            description ??= ParseDescription(listPage);

            var posterItems = listPage.DocumentNode.SelectNodes("//li[contains(concat(' ', normalize-space(@class), ' '), ' posteritem ')]");

            if (posterItems.First().HasClass("numbered-list-item"))
            {
                ranks.AddRange(posterItems.Select(item => int.Parse(item.SelectSingleNode("./p[@class='list-number']").InnerText.Trim())));
            }
            else
            {
                ranks.AddRange(Enumerable.Range(ranks.Count + 1, posterItems.Count));
            }

            letterboxdIds.AddRange(
                posterItems
                .Select(item => item.SelectSingleNode(".//div[@class='react-component']").Attributes["data-film-id"].Value));

            imdbIdTasks.AddRange(
                posterItems
                .Select(item => item.SelectSingleNode(".//div[@class='react-component']").Attributes["data-item-slug"].Value)
                .Select(async slug =>
                {
                    // We only need to parse IMDb ID from the detail page. HtmlAgilityPack will provide a more robust implementation.
                    // However, it's very memory-consuming, ~2G for a list with 800 items. So we use HttpClient directly here, which
                    // will reduce the memory usage to tens of MBs.
                    var detail = await httpClient.GetStringAsync($"/film/{slug}/", cancellationToken: cancellationToken);
                    var begin = detail.IndexOf(s_imdbBeginTag);
                    var end = detail.IndexOf('/', begin + s_imdbBeginTag.Length);
                    if (begin < 0 || end < 0)
                    {
                        throw new NotSupportedException($"Could not find IMDb ID for Letterboxd film '{slug}'. Please open an issue on GitHub and provide the slug {slug}");
                    }

                    begin += s_imdbBeginTag.Length;
                    return detail[begin..end];
                }));

            logger.Info("Parsed Letterboxd list '{0}' page {1}", listId, page);

            if (listPage.DocumentNode.SelectSingleNode("//a[@class='next']") == null)
            {
                break;
            }
            page++;
        }

        var imdbIds = await Task.WhenAll(imdbIdTasks);
        var items =
            ranks
            .Zip(letterboxdIds, imdbIds)
            .Select(tuple => new CollectionItem()
            {
                Order = tuple.First,
                // TODO: Letterboxd supports TV shows too, but all TV shows are with type film. e.g. https://letterboxd.com/slinkyman/list/letterboxds-top-100-highest-rated-documentary/
                // How can we detect them?
                Type = nameof(Movie),
                Ids = new Dictionary<string, string>()
                {
                    { "letterboxd", tuple.Second },
                    { "imdb", tuple.Third },
                },
            })
            .ToList();

        logger.Info("Completed parsing Letterboxd list '{0}', total {1} items", listId, items.Count);
        return new CollectionItemList()
        {
            Name = name ?? listId,
            Description = description ?? string.Empty,
            Ids =
            {
                { CollectorType.Letterboxd.ToProviderName(), listId.Replace('/', '\\') },
            },
            Items = items,
        };
    }

    private string ParseName(HtmlDocument doc)
    {
        var h1Node = doc.DocumentNode.SelectSingleNode("//h1[contains(concat(' ', normalize-space(@class), ' '), ' title-1 prettify ')]");
        return h1Node is null ? throw new NotSupportedException($"Could not find list name for list {listId}") : h1Node.InnerText.Trim();
    }

    private string? ParseDescription(HtmlDocument doc)
    {
        var descriptionNode = doc.DocumentNode.SelectSingleNode("//div[@id='list-notes']") ?? // For long text
            doc.DocumentNode.SelectSingleNode("//div[contains(concat(' ', normalize-space(@class), ' '), ' body-text ')]"); // For short text

        return descriptionNode
            ?.SelectNodes(".//p")
            ?.Select(p => p.InnerText)
            ?.Aggregate((a, b) => a + "\n" + b);
    }
}
