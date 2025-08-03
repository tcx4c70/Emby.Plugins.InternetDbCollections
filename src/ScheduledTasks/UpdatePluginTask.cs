namespace Emby.Plugins.InternetDbCollections.ScheduledTasks;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Emby.Plugins.InternetDbCollections.Common;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;

class UpdatePluginTask : IScheduledTask
{
    private static string PluginAssemblyName => Assembly.GetExecutingAssembly().GetName().Name + ".dll";

    private readonly ILogger _logger;
    private readonly IApplicationHost _applicationHost;
    private readonly IApplicationPaths _applicationPaths;
    private readonly IActivityManager _activityManager;
    private readonly IServerApplicationHost _serverApplicationHost;
    private readonly ILocalizationManager _localizationManager;
    private readonly IHttpClient _httpClient;

    public UpdatePluginTask(
        IApplicationHost applicationHost,
        IApplicationPaths applicationPaths,
        IActivityManager activityManager,
        IServerApplicationHost serverApplicationHost,
        ILocalizationManager localizationManager,
        IHttpClient httpClient)
    {
        _applicationHost = applicationHost;
        _applicationPaths = applicationPaths;
        _activityManager = activityManager;
        _serverApplicationHost = serverApplicationHost;
        _localizationManager = localizationManager;
        _httpClient = httpClient;
        _logger = Plugin.Instance.Logger;
    }

    public string Name => "Update Plugin";

    public string Description => "Update Plugin";

    public string Key => $"{Plugin.Instance.Name}.UpdatePlugin";

    public string Category => Plugin.Instance.Name;

    public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
    {
        await Task.Yield();
        progress.Report(0);

        try
        {
            await using var response = await _httpClient.Get(new HttpRequestOptions
            {
                Url = "https://api.github.com/repos/tcx4c70/Emby.Plugins.InternetDbCollections/releases/latest",
                AcceptHeader = "application/json",
                EnableDefaultUserAgent = true,
                CancellationToken = cancellationToken,
            }).ConfigureAwait(false);
            var apiResult = await JsonSerializer.DeserializeAsync<ApiResponseInfo>(response, cancellationToken: cancellationToken).ConfigureAwait(false);

            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            var remoteVersion = ParseVersion(apiResult?.TagName);
            if (currentVersion.CompareTo(remoteVersion) < 0)
            {
                _logger.Info("Updating plugin from version {CurrentVersion} to {RemoteVersion}", currentVersion, remoteVersion);

                var url = apiResult?.Assets
                    ?.FirstOrDefault(asset => asset.Name == PluginAssemblyName)
                    ?.BrowserDownloadUrl;
                if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    throw new Exception($"Invalid URL for plugin asset: {url}");
                }

                await using var downloadResponse = await _httpClient.Get(new HttpRequestOptions
                {
                    Url = url,
                    EnableDefaultUserAgent = true,
                    Progress = new ProgressWithBound(progress, 0, 90),
                    CancellationToken = cancellationToken,
                }).ConfigureAwait(false);
                await using var memoryStream = new MemoryStream();
                await downloadResponse.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
                memoryStream.Seek(0, SeekOrigin.Begin);
                var dllPath = Path.Combine(_applicationPaths.PluginsPath, PluginAssemblyName);
                await using var fileStream = new FileStream(dllPath, FileMode.Create, FileAccess.Write);
                await memoryStream.CopyToAsync(fileStream, 81920, cancellationToken).ConfigureAwait(false);

                _logger.Info("Plugin update complete");
                _activityManager.Create(new ActivityLogEntry
                {
                    Name = string.Format(_localizationManager.GetLocalizedString("XUpdatedOnTo"), Category, remoteVersion, _serverApplicationHost.FriendlyName),
                    Type = "PluginUpdateInstalled",
                    Overview = apiResult?.Body ?? string.Empty,
                    Severity = LogSeverity.Info,
                });
                _applicationHost.NotifyPendingRestart();
            }
        }
        catch (Exception ex)
        {
            _activityManager.Create(new ActivityLogEntry
            {
                Name = string.Format(_localizationManager.GetLocalizedString("NameInstallFailedOn"), Category, _serverApplicationHost.FriendlyName),
                Type = "PluginUpdateFailed",
                Overview = $"{ex.Message}\n{ex.StackTrace}",
                Severity = LogSeverity.Error,
            });
            _logger.ErrorException("Error while updating plugin", ex);
        }

        progress.Report(100);
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfo.TriggerDaily,
            TimeOfDayTicks = TimeSpan.FromHours(2).Ticks,
        };
    }

    private static Version ParseVersion(string v)
    {
        return new Version(v.StartsWith("v") ? v[1..] : v);
    }

    private class ApiResponseInfo
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; }

        [JsonPropertyName("body")]
        public string Body { get; set; }

        [JsonPropertyName("assets")]
        public ApiAssetInfo[] Assets { get; set; }
    }

    private class ApiAssetInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; }
    }
}
