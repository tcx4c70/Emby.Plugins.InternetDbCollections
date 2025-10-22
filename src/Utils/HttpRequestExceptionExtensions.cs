using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;

namespace Emby.Plugins.InternetDbCollections.Utils;

public static class HttpRequestExceptionExtensions
{
    public static bool IsRetryable(this HttpRequestException ex)
    {
        return ex.StatusCode?.IsRetryable() ?? ex.InnerException.IsRetryable();
    }

    private static bool IsRetryable(this Exception? ex)
    {
        if (ex is null)
        {
            return false;
        }
        if (ex is IOException || ex is HttpIOException || ex is SocketException || ex is TimeoutException)
        {
            return true;
        }
        return ex.InnerException.IsRetryable();
    }
}
