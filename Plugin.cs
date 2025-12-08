using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.KinopoiskWhiteList {
    public class Plugin : BasePlugin<PluginConfiguration> {
        public static Plugin Instance { get; private set; }
        public override string Name => "Test Plugin";
        public override string Description => "Test plugin description";
        public override Guid Id => Guid.Parse("33e6d249-648f-aaaa-a9ce-497be06c08df");

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }
    }

    public class PluginConfiguration : BasePluginConfiguration {
        public bool EnableLogging { get; set; } = true;
    }
}