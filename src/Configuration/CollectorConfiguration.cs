using System;
using Cronos;

namespace Emby.Plugins.InternetDbCollections.Configuration;

public class CollectorConfiguration
{
    public string Type { get; set; }

    // TODO: Do all types need an ID?
    public string Id { get; set; }

    public string Name { get; set; }

    public bool Enabled { get; set; } = true;

    public bool EnableTags { get; set; } = true;

    public bool EnableCollections { get; set; } = true;

    public string Schedule { get; set; } = CronExpression.Monthly.ToString();

    public DateTime? LastCollected { get; set; }

    public bool ShouldCollectNow(bool cron)
    {
        if (!Enabled || (!EnableTags && !EnableCollections))
        {
            return false;
        }
        if (!cron)
        {
            return true;
        }

        var cronExpression = CronExpression.Parse(Schedule);
        var nextOccurrence = cronExpression.GetNextOccurrence(DateTime.SpecifyKind(LastCollected ?? DateTime.MinValue, DateTimeKind.Utc));
        var now = DateTime.UtcNow;
        return nextOccurrence.HasValue && nextOccurrence <= now;
    }
}
