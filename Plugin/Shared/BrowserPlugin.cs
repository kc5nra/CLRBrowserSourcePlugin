using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Xilium.CefGlue;

namespace CLRBrowserSourcePlugin.Shared
{
    [Serializable]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class BrowserPlugin
    {

        [Category("Information")]
        [Description("The name of the plugin.")]
        [Browsable(true)]
        public String Name { get; private set; }

        [Category("Information")]
        [Description("General description of the plugin")]
        [Browsable(true)]
        public String Description { get; private set; }

        [Category("Information")]
        [Description("The path on the local machine where the plugin was loaded from.")]
        [Browsable(true)]
        public String Path { get; private set; }

        [Category("Information")]
        [Description("The version of this particular plugin")]
        [Browsable(true)]
        public String Version { get; private set; }
        
        public BrowserPlugin(CefWebPluginInfo webPluginInfo)
        {
            Name = webPluginInfo.Name;
            Description = webPluginInfo.Description;
            Path = webPluginInfo.Path;
            Version = webPluginInfo.Version;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is BrowserPlugin))
            {
                return false;
            }

            BrowserPlugin other = obj as BrowserPlugin;

            return (Name == other.Name &&
                Description == other.Description &&
                Path == other.Path &&
                Version == other.Version);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ Description.GetHashCode() ^ Path.GetHashCode() ^ ((Version != null) ? Version.GetHashCode() : 0);
        }
    }
}
