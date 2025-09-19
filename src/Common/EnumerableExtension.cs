using System.Collections.Generic;
using System.Linq;

namespace Emby.Plugins.InternetDbCollections.Common;

public static class EnumerableExtension
{
    public static IEnumerable<(TSource1, TSource2)> CartesianProduct<TSource1, TSource2>(
        this IEnumerable<TSource1> source1,
        IEnumerable<TSource2> source2)
    {
        return source1.SelectMany(item1 => source2.Select(item2 => (item1, item2)));
    }
}
