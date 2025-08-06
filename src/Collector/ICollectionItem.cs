namespace Emby.Plugins.InternetDbCollections.Collector;

public interface ICollectionItem
{
    int Order { get; }
    string Id { get; }
    string Type { get; }
}
