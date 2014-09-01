using CLROBS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xilium.CefGlue;

namespace CLRBrowserSourcePlugin.Browser
{
    internal class BrowserDisplayHandler : CefDisplayHandler
    {
        protected override void OnAddressChange(CefBrowser browser, CefFrame frame, string url)
        {
        }

        protected override void OnTitleChange(CefBrowser browser, string title)
        {
        }

        protected override bool OnTooltip(CefBrowser browser, string text)
        {
            return false;
        }

        protected override void OnStatusMessage(CefBrowser browser, string value)
        {
            if (browser == null)
            {
                API.Instance.Log("Browser null: invalid OnStatusMessage.");
                return;
            }
            API.Instance.Log("Browser {0}: Status message: {1}", browser.Identifier, value);
        }

        protected override bool OnConsoleMessage(CefBrowser browser, string message, string source, int line)
        {
            if (browser == null)
            {
                API.Instance.Log("Browser null: invalid OnConsoleMessage.");
                return false;
            }
            API.Instance.Log("Browser {0}: {1} @{2}{3}", browser.Identifier, message, source, line);
            return false;
        }
    }
}
