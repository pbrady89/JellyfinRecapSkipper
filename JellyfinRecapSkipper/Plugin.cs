using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace JellyfinRecapSkipper
{
    public class Plugin : BasePlugin<BasePluginConfiguration>
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
        }

        public override string Name => "Recap Skipper";

        public override Guid Id => new Guid("a8b6e1c2-d4f3-4e5a-9c7b-2f8d6e1a3b5c");

        public override string Description => "Detects and skips previously on recaps in TV series.";
    }

    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddSingleton<IMediaSegmentProvider, RecapSegmentProvider>();
        }
    }
}