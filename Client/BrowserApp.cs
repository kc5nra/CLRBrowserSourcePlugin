using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xilium.CefGlue;

namespace CLRBrowserSourcePlugin.Shared
{
    internal class BrowserApp : CefApp
    {
        private BrowserRenderProcessHandler renderProcessHandler;

        public BrowserApp()
        {
            renderProcessHandler = new BrowserRenderProcessHandler();
        }

        protected override void OnRegisterCustomSchemes(CefSchemeRegistrar registrar)
        {
            registrar.AddCustomScheme("http", true, true, false);
            registrar.AddCustomScheme("local", true, true, false);
        }

        private class BrowserRenderProcessHandler : CefRenderProcessHandler
        {
            protected override bool OnProcessMessageReceived(CefBrowser browser, CefProcessId sourceProcess, CefProcessMessage message)
            {
                if (message.Name == "renderProcessIdRequest")
                {
                    CefProcessMessage response = CefProcessMessage.Create("renderProcessIdResponse");
                    response.Arguments.SetInt(0, Process.GetCurrentProcess().Id);
                    browser.SendProcessMessage(CefProcessId.Browser, response);
                }

                return false;
            }
        }

        protected override CefRenderProcessHandler GetRenderProcessHandler()
        {
            return renderProcessHandler;
        }
    }
}