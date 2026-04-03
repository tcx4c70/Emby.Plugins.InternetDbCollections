namespace Emby.Plugins.InternetDbCollections.Tests;

internal sealed class RecordingProgress<T> : IProgress<T>
{
    public List<T> Values { get; } = [];

    public void Report(T value)
    {
        Values.Add(value);
    }
}
