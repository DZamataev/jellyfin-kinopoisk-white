using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace KinopoiskWhite;

using Common;

public class KinopoiskWhitePlugin : BasePlugin<KinopoiskWhitePlugin.Config>
{
    public static KinopoiskWhitePlugin Instance { get; private set; }
    public override System.Guid Id => System.Guid.Parse(Constants.ProviderGuid);
    public override string Name => Constants.ProviderName;
    public override string Description => Constants.ProviderDescription;

    public KinopoiskWhitePlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer) => Instance = this;

    public class Config : BasePluginConfiguration
    {
        public bool EnableLogging { get; set; } = true;
    }
}