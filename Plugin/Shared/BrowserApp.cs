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
        private String[] arguments;

        public BrowserApp(String[] arguments)
        {
            this.arguments = arguments;
        }

        protected override void OnRegisterCustomSchemes(CefSchemeRegistrar registrar)
        {
            registrar.AddCustomScheme("local", true, true, false);
        }

        protected override void OnBeforeCommandLineProcessing(string processType, CefCommandLine commandLine)
        {
            // only happens if configuration is bad
            if (arguments != null)
            {
                foreach (String argument in arguments)
                {
                    commandLine.AppendSwitch(argument);
                }
            }
            
        }
    }
}
