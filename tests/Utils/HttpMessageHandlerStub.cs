using System.Net;
using System.Net.Http;
using System.Text;

namespace Emby.Plugins.InternetDbCollections.Tests;

internal sealed class HttpMessageHandlerStub(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    public List<string> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request.RequestUri!.PathAndQuery);
        return Task.FromResult(responder(request));
    }
}

internal static class HttpResponseMessageFactory
{
    public static HttpResponseMessage Json(string content, params (string Name, string Value)[] headers)
    {
        return Create(content, "application/json", headers);
    }

    public static HttpResponseMessage Html(string content, params (string Name, string Value)[] headers)
    {
        return Create(content, "text/html", headers);
    }

    private static HttpResponseMessage Create(string content, string contentType, params (string Name, string Value)[] headers)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, contentType),
        };

        foreach (var header in headers)
        {
            response.Headers.TryAddWithoutValidation(header.Name, header.Value);
        }

        return response;
    }
}
