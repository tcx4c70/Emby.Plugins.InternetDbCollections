using Emby.Plugins.InternetDbCollections.Models.Collection;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Emby.Plugins.InternetDbCollections.ExternalIds;

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
