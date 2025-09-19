using System.Collections.Generic;
using MediaBrowser.Controller.Entities;

namespace Emby.Plugins.InternetDbCollections.Common;

public class BaseItemComparer : IEqualityComparer<BaseItem>
{
    public bool Equals(BaseItem x, BaseItem y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        return x.InternalId == y.InternalId;
    }

    public int GetHashCode(BaseItem obj)
    {
        if (obj is null)
        {
            return 0;
        }

        return obj.InternalId.GetHashCode();
    }
}
