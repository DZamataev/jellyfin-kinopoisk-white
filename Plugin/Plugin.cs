using System;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Entities.Movies;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.KinopoiskWhite;

public class Plugin : BasePlugin<PluginConfiguration>
{
    public static Plugin Instance { get; private set; }
    public override string Name => Constants.ProviderName;
    public override string Description => Constants.ProviderDescription;
    public override Guid Id => Guid.Parse("33e6d249-648f-aaaa-a9ce-497be06c08df");

    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }
}

public class PluginConfiguration : BasePluginConfiguration
{
    public bool EnableLogging { get; set; } = true;
}

public class KinopoiskPluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<IRemoteMetadataProvider<Movie, MovieInfo>, MetadataProvider>();
    }
}