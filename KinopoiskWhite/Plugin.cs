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

public class KinopoiskWhitePlugin : BasePlugin<KinopoiskWhitePlugin.Config>
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
    public class Config : BasePluginConfiguration
    {
        public bool EnableLogging { get; set; } = true;
    }

    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        public void RegisterServices(IServiceCollection services, IServerApplicationHost applicationHost)
        {
            GraphQL.RegisterServices(services);
            services.AddSingleton<IGraphQL, GraphQL>();
        }
    }
}