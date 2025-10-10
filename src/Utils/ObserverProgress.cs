using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Emby.Plugins.InternetDbCollections.Utils;

public class ObserverProgress<T>(IProgress<T> innerProgress) : IProgress<T> where T : INumber<T>
{
    private readonly List<ObservableProgress<T>> _observables = [];

    public void AddObservable(ObservableProgress<T> observable)
    {
        ArgumentNullException.ThrowIfNull(observable);
        _observables.Add(observable);
    }

    public void Report(T _)
    {
        var avg = _observables.Aggregate(T.Zero, (acc, observable) => acc + observable.CurrentProgress);
        avg /= (T)Convert.ChangeType(_observables.Count, typeof(T));
        innerProgress.Report(avg);
    }
}
