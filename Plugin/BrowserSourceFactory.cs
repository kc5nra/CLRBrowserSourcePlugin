using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CLROBS;

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
            return true;
        }
    }
}
