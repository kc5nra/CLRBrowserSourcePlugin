using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xilium.CefGlue;

namespace CLRBrowserSourcePlugin.RemoteBrowser
{
    class BrowserPluginVisitor : CefWebPluginInfoVisitor
    {
        protected override bool Visit(CefWebPluginInfo info, int count, int total)
        {
            return true;
        }
    }
}
