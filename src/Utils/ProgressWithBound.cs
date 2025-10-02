using System;

namespace Emby.Plugins.InternetDbCollections.Utils;

class ProgressWithBound(IProgress<double> progress, double min, double max) : IProgress<double>
{
    public void Report(double value)
    {
        value = min + value / 100.0 * (max - min);
        progress.Report(value);
    }
}
