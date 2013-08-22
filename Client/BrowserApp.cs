using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xilium.CefGlue;

namespace CLRBrowserSourcePlugin.Shared
{
    class BrowserApp : CefApp
    {
        protected override void OnRegisterCustomSchemes(CefSchemeRegistrar registrar)
        {
            registrar.AddCustomScheme("local", true, true, false);
        }
    }
}
