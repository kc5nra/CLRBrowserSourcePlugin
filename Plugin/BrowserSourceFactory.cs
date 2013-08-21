using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CLROBS;
using CLRBrowserSourcePlugin.Shared;

namespace CLRBrowserSourcePlugin
{
    class BrowserSourceFactory : AbstractImageSourceFactory
    {
        public BrowserSourceFactory()        
        {
            ClassName = "CLRBrowserSource";
            DisplayName = "CLR Browser";
        }

        public override ImageSource Create(XElement data)
        {
            return new BrowserSource(data);
        }

        public override bool ShowConfiguration(XElement data)
        {
            
            ConfigDialog dialog = new ConfigDialog(data);
            if (dialog.ShowDialog().GetValueOrDefault(false))
            {
                BrowserConfig config = new BrowserConfig();
                config.Reload(data);

                data.Parent.SetFloat("cx", (float)config.BrowserSourceSettings.Width);
                data.Parent.SetFloat("cy", (float)config.BrowserSourceSettings.Height);

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
