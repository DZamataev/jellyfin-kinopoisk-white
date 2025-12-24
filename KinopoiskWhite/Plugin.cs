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
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Entities.Movies;

namespace KinopoiskWhite;

using Api;
using Api.Models;
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
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        GraphQL.RegisterServices(serviceCollection);
        serviceCollection.AddSingleton<IGraphQL, GraphQL>();
        serviceCollection.AddSingleton<IApiService<FilmInfo>, ApiServiceMovie>();
        serviceCollection.AddSingleton<IApiService<FilmPerson>, ApiServicePerson>();

        serviceCollection.AddSingleton<IRemoteMetadataProvider<Movie, MovieInfo>,
                                       MovieMetadataProvider>();

        serviceCollection.AddSingleton<IRemoteMetadataProvider<Person, PersonLookupInfo>,
                                       PersonMetadataProvider>();
    }
}

public abstract class Base {
    #pragma warning disable CA1822 // Mark members as static
    public string Name => Constants.ProviderName;
    public string Description => Constants.ProviderDescription;
    #pragma warning restore CA1822 // Mark members as static
}

public abstract class BaseSingleton: Base {
    protected readonly ILogger _logger;
    protected readonly IHttpClientFactory _httpClientFactory;

    protected BaseSingleton (
        ILogger logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;

        _logger?.LogDebug("INIT");
    }
}
