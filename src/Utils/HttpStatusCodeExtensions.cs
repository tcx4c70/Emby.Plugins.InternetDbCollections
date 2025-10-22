using System.Net;

namespace Emby.Plugins.InternetDbCollections.Utils;

public static class HttpStatusCodeExtensions
{
    public static bool IsRetryable(this HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.RequestTimeout ||
               statusCode == HttpStatusCode.TooManyRequests ||
               statusCode == HttpStatusCode.InternalServerError ||
               statusCode == HttpStatusCode.BadGateway ||
               statusCode == HttpStatusCode.ServiceUnavailable ||
               statusCode == HttpStatusCode.GatewayTimeout;
    }

    public static bool IsSuccessful(this HttpStatusCode statusCode)
    {
        int code = (int)statusCode;
        return code >= 200 && code <= 299;
    }
}
