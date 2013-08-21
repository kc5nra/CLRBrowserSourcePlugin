using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xilium.CefGlue;

namespace CLRBrowserSourcePlugin.Browser
{
    public delegate void AfterCreatedEventHandler(CefBrowser browser);
    public delegate bool DoCloseEventHandler(CefBrowser browser);
    public delegate void OnBeforeCloseEventHandler(CefBrowser browser);

    internal class BrowserLifeSpanHandler : CefLifeSpanHandler
    {
        protected override void OnAfterCreated(CefBrowser browser)
        {
            if (AfterCreatedEvent != null)
            {
                AfterCreatedEvent(browser);
            }
        }

        protected override bool DoClose(CefBrowser browser)
        {
            if (DoCloseEvent != null)
            {
                return DoCloseEvent(browser);
            }
            return false;
        }

        protected override void OnBeforeClose(CefBrowser browser)
        {
            if (OnBeforeCloseEvent != null)
            {
                OnBeforeCloseEvent(browser);
            }
        }

        public AfterCreatedEventHandler AfterCreatedEvent { private get; set; }
        public DoCloseEventHandler DoCloseEvent { private get; set; }
        public OnBeforeCloseEventHandler OnBeforeCloseEvent { private get; set; }
    }
}
