using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CLRBrowserSourcePlugin.Shared;

using Xilium.CefGlue;

namespace CLRBrowserSourcePlugin.Browser
{
    internal class BrowserWrapper : IDisposable
    {
        private bool isDisposed;
        private BrowserClient browserClient;
        private CefBrowser browser;
        private CefBrowserHost browserHost;

        private int width;
        private int height;

        public BrowserWrapper(BrowserSource browserSource)
        {
            browserClient = new BrowserClient();
            browserClient.RenderHandler.SizeEvent = new SizeEventHandler(Size);
            browserClient.RenderHandler.PaintEvent = new PaintEventHandler(browserSource.RenderTexture);
            browserClient.RenderHandler.CreateTextureEvent = new CreateTextureEventHandler(browserSource.CreateTexture);
            browserClient.RenderHandler.DestroyTextureEvent = new DestroyTextureEventHandler(browserSource.DestroyTexture);
            browserClient.LifeSpanHandler.AfterCreatedEvent = new AfterCreatedEventHandler(AfterCreated);
        }

        public void UpdateSettings(BrowserConfig config)
        {
            width = (int)config.BrowserSourceSettings.Width;
            height = (int)config.BrowserSourceSettings.Height;

            CefWindowInfo windowInfo = CefWindowInfo.Create();
            windowInfo.TransparentPainting = true;
            windowInfo.SetAsOffScreen(IntPtr.Zero);
            windowInfo.Width = (int)width;
            windowInfo.Height = (int)height;

            BrowserInstanceSettings settings = AbstractSettings.DeepClone(BrowserSettings.Instance.InstanceSettings);
            settings.MergeWith(config.BrowserInstanceSettings);
            
            CefBrowserSettings browserSettings = new CefBrowserSettings {
                AcceleratedCompositing = settings.AcceleratedCompositing,
                ApplicationCache = settings.ApplicationCache,
                AuthorAndUserStyles = settings.AuthorAndUserStyles,
                CaretBrowsing = settings.CaretBrowsing,
                CursiveFontFamily = settings.CursiveFontFamily,
                Databases = settings.Databases,
                DefaultEncoding = settings.DefaultEncoding,
                DefaultFixedFontSize = settings.DefaultFixedFontSize,
                DefaultFontSize = settings.DefaultFontSize,
                DeveloperTools = settings.DeveloperTools,
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
                PageCache = settings.PageCache,
                Plugins = settings.Plugins,
                RemoteFonts = settings.RemoteFonts,
                SansSerifFontFamily = settings.SansSerifFontFamily,
                SerifFontFamily = settings.SerifFontFamily,
                StandardFontFamily = settings.StandardFontFamily,
                //TabToLinks = settings.TabToLinks,
                //TextAreaResize = settings.TextAreaResize,
                UniversalAccessFromFileUrls = settings.UniversalAccessFromFileUrls,
                //UserStyleSheetLocation = settings.UserStyleSheetLocation,
                WebGL = settings.WebGL,
                WebSecurity = settings.WebSecurity,
            };

            BrowserManager.Instance.Dispatcher.InvokeAsync(() =>
            {
                CefBrowserHost.CreateBrowser(windowInfo, browserClient, browserSettings, config.BrowserSourceSettings.Url);
            });
            
        }

        public bool Size(ref CefRectangle rect)
        {
            rect.X = 0;
            rect.Y = 0;
            rect.Width = width;
            rect.Height = height;

            return true;
        }

        public void AfterCreated(CefBrowser browser)
        {
            this.browser = browser;
            this.browserHost = browser.GetHost();
        }

        #region Disposable

        ~BrowserWrapper()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (browserHost != null)
                {
                    browserHost.CloseBrowser();
                    browserHost.Dispose();
                    browserHost = null;
                }

                if (browser != null)
                {
                    browser.Dispose();
                    browser = null;
                }

                if (browserClient != null)
                {
                    browserClient.Dispose();
                    browserClient = null;
                }
            }

            isDisposed = true;
        }

        #endregion
        
        #region Properties

        #endregion
    }
}
