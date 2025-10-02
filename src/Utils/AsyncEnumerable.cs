using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Emby.Plugins.InternetDbCollections.Utils;

// Why do we need this instead of System.Linq.Async?
// Emby doesn't support dependencies of plugins. We should ship a plugin with all its dependencies into a single DLL, which I don't want to do.
// TODO: Switch to System.Linq.AsyncEnumerable after upgrade to .Net 10 (https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/10.0/asyncenumerable)
public static class AsyncEnumerable
{
    public static async IAsyncEnumerable<TResoult> Select<TSource, TResoult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResoult> selector)
    {
        await foreach (var item in source)
        {
            yield return selector(item);
        }
    }

    public static async IAsyncEnumerable<TResult> Cast<TResult>(this IAsyncEnumerable<object?> source)
    {
        await foreach (var item in source)
        {
            yield return (TResult)item!;
        }
    }

    public static async ValueTask<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
    {
        var list = new List<T>();
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            list.Add(item);
        }
        return list;
    }
}
