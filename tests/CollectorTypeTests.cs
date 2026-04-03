using Emby.Plugins.InternetDbCollections.Models.Collection;

namespace Emby.Plugins.InternetDbCollections.Tests;

public class CollectorTypeTests
{
    [Theory]
    [InlineData(CollectorType.ImdbChart, "IMDbChart")]
    [InlineData(CollectorType.ImdbList, "IMDbList")]
    [InlineData(CollectorType.TraktList, "TraktList")]
    [InlineData(CollectorType.MdbList, "MDBList")]
    [InlineData(CollectorType.Letterboxd, "Letterboxd")]
    public void ToProviderName_maps_supported_collector_types(string collectorType, string expected)
    {
        Assert.Equal(expected, collectorType.ToProviderName());
    }

    [Fact]
    public void ToProviderName_throws_for_unknown_collector_type()
    {
        Assert.Throws<ArgumentException>(() => "Unknown".ToProviderName());
    }
}
