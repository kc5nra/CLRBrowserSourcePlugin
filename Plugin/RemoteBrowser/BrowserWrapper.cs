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
            width = (int)config.Width;
            height = (int)config.Height;

            
            CefWindowInfo windowInfo = CefWindowInfo.Create();
            windowInfo.TransparentPainting = true;
            windowInfo.SetAsOffScreen(IntPtr.Zero);
            windowInfo.Width = (int)config.Width;
            windowInfo.Height = (int)config.Height;

            CefBrowserSettings settings = new CefBrowserSettings {
                FileAccessFromFileUrls = CefState.Enabled,
                WebGL = CefState.Enabled,
                WebSecurity = CefState.Disabled,
                AcceleratedCompositing = CefState.Enabled
                
            };
                        
            CefBrowserHost.CreateBrowser(windowInfo, browserClient, settings, config.Url);
            
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
