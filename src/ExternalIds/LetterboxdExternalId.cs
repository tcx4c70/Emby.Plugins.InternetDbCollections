using Emby.Plugins.InternetDbCollections.Models.Collection;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Emby.Plugins.InternetDbCollections.ExternalIds;

public class LetterboxdExternalId : IExternalId
{
    public string Name => CollectorType.Letterboxd;

    public string Key => Name.ToProviderName();

    public string UrlFormatString => "https://letterboxd.com/{0}/";

    public bool Supports(IHasProviderIds item)
    {
        return item is BoxSet;
    }
}
