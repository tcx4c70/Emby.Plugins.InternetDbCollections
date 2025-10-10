using System;
using System.Numerics;

namespace Emby.Plugins.InternetDbCollections.Utils;

public class ObservableProgress<T> : IProgress<T> where T : INumber<T>
{
    public event Action<T>? ProgressChanged;

    public T CurrentProgress { get; private set; } = T.Zero;

    public void Report(T value)
    {
        CurrentProgress = value;
        ProgressChanged?.Invoke(value);
    }
}
