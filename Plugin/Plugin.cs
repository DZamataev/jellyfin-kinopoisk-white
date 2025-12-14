using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;
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

using Api;
using Common;
using Providers;

public class KinopoiskWhitePlugin : BasePlugin<KinopoiskWhitePluginConfiguration>
{
    public static KinopoiskWhitePlugin Instance { get; private set; }
    public override string Name => Constants.ProviderName;
    public override string Description => Constants.ProviderDescription;
    public override Guid Id => Guid.Parse("33e6d249-648f-aaaa-a9ce-497be06c08df");

    public KinopoiskWhitePlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }
}

public class KinopoiskWhitePluginConfiguration : BasePluginConfiguration
{
    public bool EnableLogging { get; set; } = true;
}

public record KinopoiskExternalId : IExternalId
{
    public string ProviderName => Constants.ProviderName;
    public string Key => Constants.ProviderId;
    public string UrlFormatString => "https://www.kinopoisk.ru/film/{0}";
    public ExternalIdMediaType? Type => null;
    public bool Supports(IHasProviderIds item)
    {
        return item is Movie || item is Series;
    }
}

public class KinopoiskWhitePluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton((sp) => new GraphQL(
            sp.GetRequiredService<ILogger<GraphQL>>(),
            sp.GetRequiredService<IHttpClientFactory>()
        ));
        serviceCollection.AddSingleton((sp) => new KinopoiskApi(
            sp.GetRequiredService<GraphQL>(),
            sp.GetRequiredService<ILogger<KinopoiskApi>>(),
            sp.GetRequiredService<IHttpClientFactory>()
        ));
        serviceCollection.AddSingleton<IRemoteMetadataProvider<Movie, MovieInfo>, KinopoiskItemProvider>();
    }
}