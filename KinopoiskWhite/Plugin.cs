using Microsoft.Extensions.DependencyInjection;

using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Entities.Movies;

namespace KinopoiskWhite;

using Api;
using Common;
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
    public bool Supports(IHasProviderIds item) => (
        item is Movie ||
        item is Person
    );
}

public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection service, IServerApplicationHost applicationHost)
    {
        GraphQL.RegisterServices(service);
        service.AddSingleton<IGraphQL, GraphQL>();
        service.AddSingleton<IRemoteMetadataProvider<Movie, MovieInfo>, MovieProvider>();
        // service.AddSingleton<IRemoteMetadataProvider<Person, PersonLookupInfo>, PersonProvider>();
    }
}