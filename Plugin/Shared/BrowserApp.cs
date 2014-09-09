using CLRBrowserSourcePlugin.Browser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xilium.CefGlue;

namespace CLRBrowserSourcePlugin.Shared
{
    internal class BrowserApp : CefApp
    {
        private String[] arguments;
        private BrowserProcessHandler browserProcessHandler;

        public BrowserApp(String[] arguments,
            ManualResetEventSlim contextInitializedEvent)
        {
            this.arguments = arguments;

            browserProcessHandler = new BrowserProcessHandler(
                contextInitializedEvent);
        }

        private class BrowserProcessHandler : CefBrowserProcessHandler
        {
            private ManualResetEventSlim contextInitializedEvent;

            internal BrowserProcessHandler(ManualResetEventSlim contextInitializedEvent)
            {
                this.contextInitializedEvent = contextInitializedEvent;
            }

            protected override void OnContextInitialized()
            {
                contextInitializedEvent.Set();
            }
        }

        protected override CefBrowserProcessHandler GetBrowserProcessHandler()
        {
            return browserProcessHandler;
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
                        if (argument.Length > 2)
                        {
                            normalizedArgument = argument.Substring(2);
                        }
                        else
                        {
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

            // If these were not manually specified then
            // try to add pepflashplayer.dll
            if (!commandLine.HasSwitch("ppapi-out-of-process") &&
                !commandLine.HasSwitch("register-pepper-plugins"))
            {
                string flashPluginPath = Path.Combine(
                    CLRBrowserSourcePlugin.AssemblyDirectory,
                    "CLRBrowserSourcePlugin", "pepflashplayer.dll");

                if (File.Exists(flashPluginPath))
                {
                    commandLine.AppendSwitch("ppapi-out-of-process");

                    string flashPluginValue = flashPluginPath +
                        ";application/x-shockwave-flash";

                    commandLine.AppendSwitch("register-pepper-plugins",
                        flashPluginValue);
                }
            }
        }
    }
}