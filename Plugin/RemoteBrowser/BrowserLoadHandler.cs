using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xilium.CefGlue;

namespace CLRBrowserSourcePlugin.Browser
{
    public delegate void OnLoadEndEventHandler(CefBrowser browser, CefFrame frame, int httpStatusCode);

    internal class BrowserLoadHandler : CefLoadHandler
    {
        protected override void OnLoadEnd(CefBrowser browser, CefFrame frame, int httpStatusCode)
        {
            if (OnLoadEndEvent != null)
            {
                OnLoadEndEvent(browser, frame, httpStatusCode);
            }
        }

        public OnLoadEndEventHandler OnLoadEndEvent { private get; set; }
    }
}