using CLRBrowserSourcePlugin.RemoteBrowser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xilium.CefGlue;

namespace CLRBrowserSourcePlugin.Browser
{
    internal class BrowserClient : CefClient
    {
        public BrowserLifeSpanHandler LifeSpanHandler { get; set; }

        public BrowserDisplayHandler DisplayHandler { get; set; }

        public BrowserRenderHandler RenderHandler { get; set; }

        public BrowserLoadHandler LoadHandler { get; set; }

        public BrowserClient()
        {
            LifeSpanHandler = new BrowserLifeSpanHandler();
            LoadHandler = new BrowserLoadHandler();
            DisplayHandler = new BrowserDisplayHandler();
            RenderHandler = new BrowserRenderHandler();
        }

        protected override CefLifeSpanHandler GetLifeSpanHandler()
        {
            return LifeSpanHandler;
        }

        protected override CefDisplayHandler GetDisplayHandler()
        {
            return DisplayHandler;
        }

        protected override CefRenderHandler GetRenderHandler()
        {
            return RenderHandler;
        }

        protected override CefLoadHandler GetLoadHandler()
        {
            return LoadHandler;
        }

        protected override bool OnProcessMessageReceived(CefBrowser browser, CefProcessId sourceProcess, CefProcessMessage message)
        {
            if (message.Name == "renderProcessIdResponse")
            {
                int renderProcessId = message.Arguments.GetInt(0);
                BrowserWrapper browserWrapper;
                if (BrowserManager.Instance.TryGetBrowser(
                    browser.Identifier, out browserWrapper))
                {
                    browserWrapper.UpdateRenderProcessId(renderProcessId);
                }
            }

            return false;
        }
    }
}