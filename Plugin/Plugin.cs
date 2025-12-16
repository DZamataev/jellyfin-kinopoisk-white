using Microsoft.Extensions.DependencyInjection;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;

namespace Plugin;

using Common;
using Plugin.Api;
using Providers;

public class KinopoiskWhitePlugin : BasePlugin<PluginConfiguration>
{
    public static KinopoiskWhitePlugin Instance { get; private set; }
    public override string Name => Constants.ProviderName;
    public override string Description => Constants.ProviderDescription;
    public override System.Guid Id => System.Guid.Parse("33e6d249-648f-aaaa-a9ce-497be06c08df");

    public KinopoiskWhitePlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }
}

public class PluginConfiguration : BasePluginConfiguration
{
    public bool EnableLogging { get; set; } = true;
}

public record ExternalId : IExternalId
{
    public string Key => Constants.ProviderId;
    public string ProviderName => Constants.ProviderName;
    public string UrlFormatString => "https://www.kinopoisk.ru/film/{0}";
    public ExternalIdMediaType? Type => null;
    public bool Supports(IHasProviderIds item) => item is Movie || item is Series;
}

public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<GraphQL>();
        serviceCollection.AddSingleton<KinopoiskApi>();
        serviceCollection.AddSingleton<IRemoteImageProvider, RemoteImageProvider<Movie>>();
        serviceCollection.AddSingleton<IRemoteMetadataProvider<Movie, MovieInfo>, MovieMetadataProvider>();
    }
}