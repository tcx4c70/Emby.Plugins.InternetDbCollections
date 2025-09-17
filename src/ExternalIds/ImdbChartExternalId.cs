namespace Emby.Plugins.InternetDbCollections.ExternalIds;

using Emby.Plugins.InternetDbCollections.Common;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

public class ImdbChartExternalId : IExternalId
{
    public string Name => CollectorType.ImdbChart;

    public string Key => Name.ToProviderName();

    public string UrlFormatString => "https://www.imdb.com/chart/{0}/";

    public bool Supports(IHasProviderIds item)
    {
        return item is BoxSet;
    }
}
