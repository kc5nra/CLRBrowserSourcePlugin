using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xilium.CefGlue;

namespace CLRBrowserSourcePlugin.Browser
{
    public delegate void AfterCreatedEventHandler(CefBrowser browser);

    internal class BrowserLifeSpanHandler : CefLifeSpanHandler
    {
        private AfterCreatedEventHandler afterCreatedEventHandler;

        protected override void OnAfterCreated(CefBrowser browser)
        {
            afterCreatedEventHandler(browser);
        }

        public AfterCreatedEventHandler AfterCreatedEvent
        {
            set
            {
                afterCreatedEventHandler = value;
            }
        }
    }
}
