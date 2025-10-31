using System;
using System.Collections.Concurrent;
using System.Net.Http;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace Emby.Plugins.InternetDbCollections.Utils;

public sealed class HttpClientPool
{
    private readonly ConcurrentDictionary<string, HttpClient> _clients = new();

    public static HttpClientPool Instance { get; } = new HttpClientPool();

    public HttpClient GetClient(string name, Action<HttpClient>? configureClient = null)
    {
        return _clients.GetOrAdd(name, _ => CreateClient(configureClient));
    }

    private static HttpClient CreateClient(Action<HttpClient>? configureClient = null)
    {
        var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new HttpRetryStrategyOptions()
            {
                MaxRetryAttempts = 20,
                Delay = TimeSpan.FromMilliseconds(100),
                MaxDelay = TimeSpan.FromSeconds(10),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
            })
            .Build();
        var socketHandler = new SocketsHttpHandler();
        var handler = new ResilienceHandler(pipeline)
        {
            InnerHandler = socketHandler,
        };
        var client = new HttpClient(handler);
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:x.x.x) Gecko/20041107 Firefox/x.x");
        client.Timeout = TimeSpan.FromSeconds(30);
        configureClient?.Invoke(client);
        return client;
    }
}
