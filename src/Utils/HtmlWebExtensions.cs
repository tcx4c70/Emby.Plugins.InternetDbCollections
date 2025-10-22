using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace Emby.Plugins.InternetDbCollections.Utils;

public static class HtmlWebExtensions
{
    public static async Task<HtmlDocument> LoadFromWebAsync(this HtmlWeb web, string url, int retryCount, CancellationToken cancellationToken = default)
    {
        // Use Polly?
        int sleepTime = 10;
        for (int i = 0; i < retryCount; i++)
        {
            try
            {
                var doc = await web.LoadFromWebAsync(url, cancellationToken);
                web.EnsureSuccessStatusCode();
                return doc;
            }
            catch (HttpRequestException ex) when (ex.IsRetryable())
            {
                await Task.Delay(sleepTime, cancellationToken);
                sleepTime *= 2;
            }
        }
        throw new HttpRequestException($"Fail to fetch after {retryCount} retries, last status code: {web.StatusCode}", inner: null, web.StatusCode);
    }

    public static HtmlWeb EnsureSuccessStatusCode(this HtmlWeb web)
    {
        if (!web.StatusCode.IsSuccessful())
        {
            throw new HttpRequestException($"Response status code does not indicate success: {web.StatusCode}", inner: null, web.StatusCode);
        }
        return web;
    }
}

