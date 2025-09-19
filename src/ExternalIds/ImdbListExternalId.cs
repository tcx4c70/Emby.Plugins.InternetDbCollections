using Emby.Plugins.InternetDbCollections.Common;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Emby.Plugins.InternetDbCollections.ExternalIds;

public class ImdbListExternalId : IExternalId
{
    public string Name => CollectorType.ImdbList;

    public string Key => Name.ToProviderName();

    public string UrlFormatString => "https://www.imdb.com/list/{0}/";

    public bool Supports(IHasProviderIds item)
    {
        return item is BoxSet;
    }
}
