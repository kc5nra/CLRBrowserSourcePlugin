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
        
        public BrowserClient()
        {
            
            LifeSpanHandler = new BrowserLifeSpanHandler();
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
    }
}
