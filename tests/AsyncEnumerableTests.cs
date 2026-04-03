using Emby.Plugins.InternetDbCollections.Utils;

namespace Emby.Plugins.InternetDbCollections.Tests;

public class AsyncEnumerableTests
{
    [Fact]
    public async Task Select_and_ToListAsync_maps_async_sequences()
    {
        var result = await Numbers()
            .Select(x => x * 2)
            .ToListAsync();

        Assert.Equal([2, 4, 6], result);
    }

    private static async IAsyncEnumerable<int> Numbers()
    {
        yield return 1;
        await Task.Yield();
        yield return 2;
        yield return 3;
    }
}
