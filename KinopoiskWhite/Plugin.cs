using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace KinopoiskWhite;

using System.Collections.Generic;
using Common;

public class KinopoiskWhitePlugin : BasePlugin<KinopoiskWhitePlugin.Config>, IHasWebPages
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

    public IEnumerable<PluginPageInfo> GetPages() {
        yield return new PluginPageInfo
        {
            Name = Name,
            EmbeddedResourcePath = GetType().Namespace + ".Common.config.html"
        };
    }
}