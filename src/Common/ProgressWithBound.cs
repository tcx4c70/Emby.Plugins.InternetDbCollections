using System;

namespace Emby.Plugins.InternetDbCollections.Common;

class ProgressWithBound : IProgress<double>
{
    private readonly IProgress<double> _progress;
    private readonly double _min;
    private readonly double _max;

    public ProgressWithBound(IProgress<double> progress, double min, double max)
    {
        _progress = progress;
        _min = min;
        _max = max;
    }

    public void Report(double value)
    {
        value = _min + value / 100.0 * (_max - _min);
        _progress.Report(value);
    }
}
