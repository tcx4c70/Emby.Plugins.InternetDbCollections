using Emby.Plugins.InternetDbCollections.Utils;

namespace Emby.Plugins.InternetDbCollections.Tests;

public class ProgressTests
{
    [Theory]
    [InlineData(0, 10, 20, 10)]
    [InlineData(50, 10, 20, 15)]
    [InlineData(100, 10, 20, 20)]
    public void ProgressWithBound_maps_values_into_the_target_range(double value, double min, double max, double expected)
    {
        var recorder = new RecordingProgress<double>();
        var progress = new ProgressWithBound(recorder, min, max);

        progress.Report(value);

        Assert.Single(recorder.Values);
        Assert.Equal(expected, recorder.Values[0], 10);
    }

    [Fact]
    public void ObserverProgress_reports_the_average_of_all_observables()
    {
        var recorder = new RecordingProgress<double>();
        var observer = new ObserverProgress<double>(recorder);
        var first = new ObservableProgress<double>();
        var second = new ObservableProgress<double>();

        observer.AddObservable(first);
        observer.AddObservable(second);

        first.Report(25);
        second.Report(75);
        observer.Report(0);

        Assert.Single(recorder.Values);
        Assert.Equal(50, recorder.Values[0], 10);
    }
}
