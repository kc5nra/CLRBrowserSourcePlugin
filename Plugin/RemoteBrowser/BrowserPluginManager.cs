using CLRBrowserSourcePlugin.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xilium.CefGlue;

namespace CLRBrowserSourcePlugin.RemoteBrowser
{
    class BrowserPluginManager
    {
        internal bool IsInitialized { get; private set; }

        private List<BrowserPlugin> initialPlugins;

        internal BrowserPluginManager()
        {

        }

        internal void Initialize()
        {
            if (IsInitialized)
            {
                return; 
            }

            initialPlugins = new List<BrowserPlugin>();

            List<String> disabledPlugins = BrowserSettings.Instance.PluginSettings.DisabledPlugins;

            BrowserPluginVisitor.Visit(new Action<CefWebPluginInfo>((pluginInfo) =>
            {
                initialPlugins.Add(new BrowserPlugin(pluginInfo));
                if (disabledPlugins.Contains(pluginInfo.Name)) 
                {
                    CefRuntime.UnregisterInternalWebPlugin(pluginInfo.Path);
                }
            }));

            BrowserSettings.Instance.RuntimeSettings.Plugins.AddRange(initialPlugins);
        }
    }
}
