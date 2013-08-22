using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CLRBrowserSourcePlugin.Shared;

using Xilium.CefGlue;
using System.Diagnostics;
using CLROBS;
using System.Threading;

namespace CLRBrowserSourcePlugin.Browser
{
    internal class BrowserWrapper : IDisposable
    {
        private bool isDisposed;
        private BrowserClient browserClient;
        private CefBrowser browser;
        private CefBrowserHost browserHost;

        private BrowserConfig config;

        private int width;
        private int height;

        bool isStarted = false;
        bool isClosed = false;

        public BrowserWrapper(BrowserSource browserSource)
        {
            browserClient = new BrowserClient();
            browserClient.RenderHandler.SizeEvent = new SizeEventHandler(Size);
            browserClient.RenderHandler.PaintEvent = new PaintEventHandler(browserSource.RenderTexture);
            browserClient.RenderHandler.CreateTextureEvent = new CreateTextureEventHandler(browserSource.CreateTexture);
            browserClient.RenderHandler.DestroyTextureEvent = new DestroyTextureEventHandler(browserSource.DestroyTexture);
            browserClient.LifeSpanHandler.AfterCreatedEvent = new AfterCreatedEventHandler(AfterCreated);
            browserClient.LifeSpanHandler.OnBeforeCloseEvent = new OnBeforeCloseEventHandler(OnBeforeClose);
        }

        public void UpdateSettings(BrowserConfig config)
        {
            this.config = config;

            width = (int)config.BrowserSourceSettings.Width;
            height = (int)config.BrowserSourceSettings.Height;

            CefWindowInfo windowInfo = CefWindowInfo.Create();
            windowInfo.TransparentPainting = true;
            windowInfo.SetAsOffScreen(IntPtr.Zero);
            windowInfo.Width = (int)width;
            windowInfo.Height = (int)height;

            String base64EncodedDataUri = "data:text/css;charset=utf-8;base64,";
            String base64EncodedCss = Convert.ToBase64String(Encoding.UTF8.GetBytes(config.BrowserSourceSettings.CSS));
            
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
                UserStyleSheetLocation = base64EncodedDataUri + base64EncodedCss,
                WebGL = settings.WebGL,
                WebSecurity = settings.WebSecurity,
            };

            String url = config.BrowserSourceSettings.Url;

            if (config.BrowserSourceSettings.IsApplyingTemplate)
            {
                String resolvedTemplate = config.BrowserSourceSettings.Template;
                resolvedTemplate = resolvedTemplate.Replace("$(FILE)", config.BrowserSourceSettings.Url);
                resolvedTemplate = resolvedTemplate.Replace("$(WIDTH)", config.BrowserSourceSettings.Width.ToString());
                resolvedTemplate = resolvedTemplate.Replace("$(HEIGHT)", config.BrowserSourceSettings.Height.ToString());

                url = "local://initial/";
            }

            // must be sync invoke because wrapper can be destroyed before it is run
            BrowserManager.Instance.Dispatcher.Invoke(() =>
            {
                CefBrowserHost.CreateBrowser(windowInfo, browserClient, browserSettings, url);
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
            if (browser != null)
            {
                this.browser = browser;
                this.browserHost = browser.GetHost();

                BrowserManager.Instance.RegisterBrowser(browser.Identifier, config);
                isStarted = true;
            }
        }

        public void OnBeforeClose(CefBrowser browser)
        {
            isClosed = true;
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
                BrowserManager.Instance.Dispatcher.Invoke(() =>
                {

                    Stopwatch stopwatch = new Stopwatch();

                    stopwatch.Start();
                    while (!isStarted)
                    {
                        if (stopwatch.ElapsedMilliseconds > 500)
                        {
                            API.Instance.Log("BrowserWrapper::Dispose timed out waiting for browser to start (required for safe disposal); Attempting to continue");
                            break;
                        }
                        Thread.Sleep(10);
                    }
                    stopwatch.Stop();
                    stopwatch.Reset();

                    if (browserHost != null)
                    {
                        browserHost.CloseBrowser(true);
                        // OnBeforeClose must be called before we start disposing
                        stopwatch.Start();
                        while (!isClosed)
                        {
                            if (stopwatch.ElapsedMilliseconds > 500)
                            {
                                API.Instance.Log("BrowserWrapper::Dispose timed out waiting for browser to close (required for safe disposal); Attempting unsafe kill");
                                break;
                            }
                            Thread.Sleep(10);
                        }
                        stopwatch.Stop();
                        browserHost.ParentWindowWillClose();
                        browserHost.Dispose();
                        browserHost = null;
                    }

                    if (browser != null)
                    {
                        BrowserManager.Instance.UnregisterBrowser(browser.Identifier);
                        browser.Dispose();
                        browser = null;
                    }

                    if (browserClient != null)
                    {
                        browserClient.Dispose();
                        browserClient = null;
                    }
                });
            }

            isDisposed = true;
        }

        #endregion
        
        #region Properties

        #endregion
    }
}
