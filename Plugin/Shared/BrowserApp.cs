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
            registrar.AddCustomScheme("http", true, true, false);
        }

        protected override void OnBeforeCommandLineProcessing(string processType, CefCommandLine commandLine)
        {

            // only happens if configuration is bad
            if (arguments != null)
            {
                foreach (String argument in arguments)
                {
                    string normalizedArgument = argument;
                    if (argument.StartsWith("--"))
                    {
                        if (argument.Length > 2) {
                            normalizedArgument = argument.Substring(2);
                        } else {
                            Console.WriteLine(
                                "BrowserApp::OnBeforeCommandLineProcessing bad argument {0}",
                                argument);
                            continue;
                        }
                    }

                    int argSplitIndex = normalizedArgument.IndexOf('=');
                    if (argSplitIndex >= 0)
                    {
                        string name = normalizedArgument.Substring(0, argSplitIndex);
                        string value = normalizedArgument.Substring(argSplitIndex + 1);
                        
                        if (value.StartsWith("\"") && value.EndsWith("\""))
                        {
                            value = value.Substring(1, value.Length - 2);
                        }
                        
                        commandLine.AppendSwitch(name, value);
                    }
                    else
                    {
                        commandLine.AppendSwitch(normalizedArgument);
                    }
                }
            }
            
        }
    }
}
