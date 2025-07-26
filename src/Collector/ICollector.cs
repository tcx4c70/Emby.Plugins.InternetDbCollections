namespace Emby.Plugins.InternetDbCollections.Collector;

using System;
using System.Threading;
using System.Threading.Tasks;

interface ICollector
{
    /// <summary>
    /// The name of the collector.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes the collector.
    /// This method must be called once at the start of the collection process. It should set up any necessary resources
    /// or state (e.g., scrape from the internet database to initialize name, description, and metadata).
    /// </summary>
    public Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Update Emby metadata.
    /// If the metadata might change such as IMDb TOP 250, the method should cleanup old metadata then add new.
    /// </summary>
    public Task UpdateMetadataAsync(IProgress<double> progress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up Emby metadata.
    /// </summary>
    public Task CleanupMetadataAsync(IProgress<double> progress, CancellationToken cancellationToken = default);
}
