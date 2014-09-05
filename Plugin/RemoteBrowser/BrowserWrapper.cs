using CLRBrowserSourcePlugin.Shared;
using CLROBS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Xilium.CefGlue;

namespace CLRBrowserSourcePlugin.Browser
{
    internal class BrowserWrapper
    {
        private BrowserClient browserClient;
        private CefBrowser browser;

        public BrowserConfig BrowserConfig { get; private set; }

        public BrowserWrapper()
        {
            browserClient = null;
            browser = null;
        }

        private void InitClient(BrowserSource browserSource)
        {
            Debug.Assert(browserClient == null);

            browserClient = new BrowserClient();

            browserClient.LoadHandler.OnLoadEndEvent =
                new OnLoadEndEventHandler(OnLoadEnd);
            browserClient.RenderHandler.SizeEvent =
                new SizeEventHandler(Size);
            browserClient.RenderHandler.PaintEvent =
                new PaintEventHandler(browserSource.RenderTexture);
            browserClient.RenderHandler.CreateTextureEvent =
                new CreateTextureEventHandler(browserSource.CreateTexture);
            browserClient.RenderHandler.DestroyTextureEvent =
                new DestroyTextureEventHandler(browserSource.DestroyTexture);
        }

        private void UninitClient()
        {
            Debug.Assert(browserClient != null);

            browserClient.LoadHandler = null;
            browserClient.DisplayHandler = null;
            browserClient.RenderHandler.Cleanup();
            browserClient.RenderHandler = null;

            browserClient = null;
        }

        public bool CreateBrowser(BrowserSource browserSource,
            BrowserConfig browserConfig)
        {
            if (browserClient == null)
            {
                InitClient(browserSource);
            }

            Debug.Assert(browserClient != null);
            Debug.Assert(browserConfig != null);

            BrowserConfig = browserConfig;

            CefWindowInfo windowInfo = CefWindowInfo.Create();
            windowInfo.Width = (int)browserConfig.BrowserSourceSettings.Width;
            windowInfo.Height = (int)browserConfig.BrowserSourceSettings.Height;
            windowInfo.SetAsWindowless(IntPtr.Zero, true);

            BrowserInstanceSettings settings = AbstractSettings.DeepClone(
                BrowserSettings.Instance.InstanceSettings);
            settings.MergeWith(browserConfig.BrowserInstanceSettings);

            CefBrowserSettings browserSettings = new CefBrowserSettings
            {
                WindowlessFrameRate = browserConfig.BrowserSourceSettings.Fps,
                ApplicationCache = settings.ApplicationCache,
                CaretBrowsing = settings.CaretBrowsing,
                CursiveFontFamily = settings.CursiveFontFamily,
                Databases = settings.Databases,
                DefaultEncoding = settings.DefaultEncoding,
                DefaultFixedFontSize = settings.DefaultFixedFontSize,
                DefaultFontSize = settings.DefaultFontSize,
                FantasyFontFamily = settings.FantasyFontFamily,
                FileAccessFromFileUrls = settings.FileAccessFromFileUrls,
                FixedFontFamily = settings.FixedFontFamily,
                ImageLoading = settings.ImageLoading,
                ImageShrinkStandaloneToFit = settings.ImageShrinkStandaloneToFit,
                Java = settings.Java,
                JavaScript = settings.JavaScript,
                JavaScriptAccessClipboard = settings.JavaScriptAccessClipboard,
                JavaScriptCloseWindows = settings.JavaScriptCloseWindows,
                JavaScriptDomPaste = settings.JavaScriptDomPaste,
                JavaScriptOpenWindows = settings.JavaScriptOpenWindows,
                LocalStorage = settings.LocalStorage,
                MinimumFontSize = settings.MinimumFontSize,
                MinimumLogicalFontSize = settings.MinimumLogicalFontSize,
                Plugins = settings.Plugins,
                RemoteFonts = settings.RemoteFonts,
                SansSerifFontFamily = settings.SansSerifFontFamily,
                SerifFontFamily = settings.SerifFontFamily,
                StandardFontFamily = settings.StandardFontFamily,
                //TabToLinks = settings.TabToLinks,
                //TextAreaResize = settings.TextAreaResize,
                UniversalAccessFromFileUrls =
                    settings.UniversalAccessFromFileUrls,
                WebGL = settings.WebGL,
                WebSecurity = settings.WebSecurity
            };

            String url = browserConfig.BrowserSourceSettings.Url;

            if (browserConfig.BrowserSourceSettings.IsApplyingTemplate)
            {
                String resolvedTemplate =
                    browserConfig.BrowserSourceSettings.Template;
                resolvedTemplate = resolvedTemplate.Replace("$(FILE)",
                    browserConfig.BrowserSourceSettings.Url);
                resolvedTemplate = resolvedTemplate.Replace("$(WIDTH)",
                    browserConfig.BrowserSourceSettings.Width.ToString());
                resolvedTemplate = resolvedTemplate.Replace("$(HEIGHT)",
                    browserConfig.BrowserSourceSettings.Height.ToString());

                url = "http://absolute";
            }

            ManualResetEventSlim createdBrowserEvent =
                new ManualResetEventSlim();
            CefRuntime.PostTask(CefThreadId.UI, BrowserTask.Create(() =>
            {
                try
                {
                    browser = CefBrowserHost.CreateBrowserSync(windowInfo,
                        browserClient, browserSettings, new Uri(url));
                    BrowserManager.Instance.RegisterBrowser(browser.Identifier,
                        this);
                }
                catch (Exception)
                {
                    browser = null;
                }
                finally
                {
                    createdBrowserEvent.Set();
                }
            }));

            createdBrowserEvent.Wait();

            return browser != null;
        }

        public void CloseBrowser(bool isForcingClose)
        {
            ManualResetEvent closeFinishedEvent = new ManualResetEvent(false);

            CefRuntime.PostTask(CefThreadId.UI, BrowserTask.Create(() =>
            {
                if (browser != null)
                {
                    browser.GetHost().CloseBrowser(isForcingClose);
                    BrowserManager.Instance.UnregisterBrowser(
                        browser.Identifier);
                    UninitClient();
                    browser = null;
                }

                closeFinishedEvent.Set();
            }));

            closeFinishedEvent.WaitOne();
        }

        public void OnLoadEnd(CefBrowser browser, CefFrame frame,
            int httpStatusCode)
        {
            // main frame
            if (frame.IsMain)
            {
                string base64EncodedCss = "data:text/css;charset=utf-8;base64,";
                base64EncodedCss +=
                    Convert.ToBase64String(Encoding.UTF8.GetBytes(
                        BrowserConfig.BrowserSourceSettings.CSS));

                string script = ""
                    + "var link = document.createElement('link');"
                    + "link.setAttribute('rel', 'stylesheet');"
                    + "link.setAttribute('type', 'text/css');"
                    + "link.setAttribute('href', '{0}');"
                    + "document.getElementsByTagName('head')[0].appendChild(link);";

                frame.ExecuteJavaScript(String.Format(script, base64EncodedCss),
                    null, 0);
            }
        }

        public bool Size(ref CefRectangle rect)
        {
            rect.X = 0;
            rect.Y = 0;
            rect.Width = BrowserConfig.BrowserSourceSettings.Width;
            rect.Height = BrowserConfig.BrowserSourceSettings.Height;

            return true;
        }
    }
}