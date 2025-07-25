namespace Emby.Plugins.InternetDbCollections;

using MediaBrowser.Common.Plugins;
using System;

public class Plugin : BasePlugin
{
    private readonly Guid _id = new("1B55EFD5-6080-4207-BCF8-DC2723C7AC10");

    public override string Name => "InternetDbCollections";

    public override string Description => "Internet Database Collections for Emby";

    public override Guid Id => _id;
}
