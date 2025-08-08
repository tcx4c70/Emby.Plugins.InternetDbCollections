namespace Empy.Plugins.InternetDbCollections.ExternalIds;

using Emby.Plugins.InternetDbCollections.Common;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

public class TraktListExternalId : IExternalId
{
    public string Name => CollectorType.TraktList;

    public string Key => Name.ToProviderName();

    public string UrlFormatString => "https://trakt.tv/lists/{0}";

    public bool Supports(IHasProviderIds item)
    {
        return item is BoxSet;
    }
}
