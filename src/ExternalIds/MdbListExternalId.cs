using Emby.Plugins.InternetDbCollections.Common;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Emby.Plugins.InternetDbCollections.ExternalIds;

public class MdbListExternalId : IExternalId
{
    public string Name => CollectorType.MdbList;

    public string Key => Name.ToProviderName();

    public string UrlFormatString => "https://mdblist.com/lists/{0}";

    public bool Supports(IHasProviderIds item)
    {
        return item is BoxSet;
    }
}
