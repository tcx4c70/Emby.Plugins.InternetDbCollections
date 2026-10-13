using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugins.InternetDbCollections.Models.Collection;
using Emby.Plugins.InternetDbCollections.Utils;
using HtmlAgilityPack;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Logging;

namespace Emby.Plugins.InternetDbCollections.Collector;

class LetterboxdCollector(string listId, ILogger logger, HttpClient? httpClient = null) : ICollector
{
    private static readonly string s_baseUrl = "https://letterboxd.com";
    private static readonly string s_imdbBeginTag = "www.imdb.com/title/";
    private readonly HttpClient _httpClient = httpClient ?? HttpClientPool.Instance.GetClient("letterboxd", client =>
    {
        client.BaseAddress = new Uri("https://letterboxd.com");
    });

    public async Task<CollectionItemList> CollectAsync(CancellationToken cancellationToken = default)
    {
        var page = 1;
        string? name = null;
        string? description = null;
        var ranks = new List<int>();
        var imdbIdTasks = new List<Task<string?>>();

        while (true)
        {
            var url = $"{s_baseUrl}/{listId}/page/{page}/";
            logger.Debug("Fetching Letterboxd list '{0}' page {1} data from {2}", listId, page, url);
            var htmlContent = await _httpClient.GetStringAsync($"/{listId}/page/{page}/", cancellationToken: cancellationToken);
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

            imdbIdTasks.AddRange(
                posterItems
                .Select(item => item.SelectSingleNode(".//div[@class='react-component']").Attributes["data-item-slug"].Value)
                .Select(async slug =>
                {
                    // We only need to parse IMDb ID from the detail page. HtmlAgilityPack will provide a more robust implementation.
                    // However, it's very memory-consuming, ~2G for a list with 800 items. So we use HttpClient directly here, which
                    // will reduce the memory usage to tens of MBs.
                    var detail = await _httpClient.GetStringAsync($"/film/{slug}/", cancellationToken: cancellationToken);
                    int begin = detail.IndexOf(s_imdbBeginTag, StringComparison.Ordinal);
                    if (begin >= 0)
                    {
                        begin += s_imdbBeginTag.Length;
                        int end = detail.IndexOf('/', begin);
                        if (end > begin)
                        {
                            return detail[begin..end];
                        }
                    }

                    logger.Warn(
                        "Could not find IMDb ID for Letterboxd film '{0}', keeping the item without provider IDs", slug);
                    return null;
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
            .Zip(imdbIds)
            .Select(tuple => new CollectionItem()
            {
                Order = tuple.First,
                // TODO: Letterboxd supports TV shows too, but all TV shows are with type film. e.g. https://letterboxd.com/slinkyman/list/letterboxds-top-100-highest-rated-documentary/
                // How can we detect them?
                Type = nameof(Movie),
                Ids = tuple.Second is { } imdbId
                    ? new Dictionary<string, string>()
                    {
                        { "imdb", imdbId },
                    }
                    : new Dictionary<string, string>(),
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
