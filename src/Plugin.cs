namespace Emby.Plugins.InternetDbCollections;

using Emby.Plugins.InternetDbCollections.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasThumbImage
{
    public static Plugin Instance { get; private set; }

    public readonly ILogger Logger;

    private readonly Guid _id = new("1B55EFD5-6080-4207-BCF8-DC2723C7AC10");

    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILogManager logManager)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        Logger = logManager.GetLogger(Name);
        Logger.Info("Plugin Loaded");
    }

    public override string Name => "InternetDbCollections";

    public override string Description => "Internet Database Collections for Emby";

    public override Guid Id => _id;

    public ImageFormat ThumbImageFormat => ImageFormat.Png;

    public Stream GetThumbImage()
    {
        var type = GetType();
        return type.Assembly.GetManifestResourceStream($"{type.Namespace}.Resources.thumb.png");
    }

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html",
            },
            new PluginPageInfo
            {
                Name = $"{Name}js",
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.js",
            }
        };
    }
}
