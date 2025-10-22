using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;

namespace Emby.Plugins.InternetDbCollections.Utils;

public static class HttpClientExtensions
{
    public static async Task<HttpResponseMessage> GetAsync(this HttpClient httpClient, string requestUri, int retryCount, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        // Use Polly or Microsoft.Extensions.Http.Resilience?
        int sleepTime = 10;
        for (int i = 0; i < retryCount; i++)
        {
            try
            {
                var response = await httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                return response;
            }
            catch (HttpRequestException ex) when (ex.IsRetryable())
            {
                logger?.ErrorException($"GET request to {requestUri} failed on attempt {i + 1}. Retrying in {sleepTime}ms.", ex);
                await Task.Delay(sleepTime, cancellationToken).ConfigureAwait(false);
                sleepTime *= 2;
            }
        }
        throw new HttpRequestException($"Failed to GET {requestUri} after {retryCount} attempts.");
    }

    public static async Task<string> GetStringAsync(this HttpClient httpClient, string requestUri, int retryCount, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(requestUri, retryCount, cancellationToken: cancellationToken).ConfigureAwait(false);
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async Task<Stream> GetStreamAsync(this HttpClient httpClient, string requestUri, int retryCount, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(requestUri, retryCount, cancellationToken: cancellationToken).ConfigureAwait(false);
        return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
    }
}
