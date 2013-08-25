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
    internal class BrowserWrapper
    {
        public enum BrowserStatus
        {
            Initial,
            Creating,
            Created,
            Closing,
            Closed
        }

        public BrowserStatus Status { get; private set; }
        
        private bool isDisposed;

        private bool hasPendingClose;

        private BrowserClient browserClient;
        private CefBrowser browser;
        private CefBrowserHost browserHost;
        public BrowserConfig BrowserConfig { get; private set; }


        public BrowserWrapper()
        {
            isDisposed = false;

            Status = BrowserStatus.Initial;

            hasPendingClose = false;
            browserClient = null;
            browser = null;
            browserHost = null;
        }

        private void InitClient(BrowserSource browserSource)
        {
            Debug.Assert(browserClient == null);

            browserClient = new BrowserClient();

            browserClient.RenderHandler.SizeEvent = new SizeEventHandler(Size);
            browserClient.RenderHandler.PaintEvent = new PaintEventHandler(browserSource.RenderTexture);
            browserClient.RenderHandler.CreateTextureEvent = new CreateTextureEventHandler(browserSource.CreateTexture);
            browserClient.RenderHandler.DestroyTextureEvent = new DestroyTextureEventHandler(browserSource.DestroyTexture);
            browserClient.LifeSpanHandler.AfterCreatedEvent = new AfterCreatedEventHandler(AfterCreated);
            browserClient.LifeSpanHandler.OnBeforeCloseEvent = new OnBeforeCloseEventHandler(OnBeforeClose);
            browserClient.LifeSpanHandler.DoCloseEvent = new DoCloseEventHandler(DoClose);
        }

        private void UninitClient()
        {
            Debug.Assert(browserClient != null);

            browserClient.DisplayHandler = null;
            browserClient.LifeSpanHandler = null;

            browserClient.RenderHandler.Cleanup();
            browserClient.RenderHandler = null;
        }


        public bool CreateBrowser(BrowserSource browserSource, BrowserConfig browserConfig)
        {
            Debug.Assert(Status == BrowserStatus.Initial);

            InitClient(browserSource);

            Debug.Assert(browserClient != null);
            Debug.Assert(browserConfig != null);

            BrowserConfig = browserConfig;

            CefWindowInfo windowInfo = CefWindowInfo.Create();
            windowInfo.TransparentPainting = true;
            windowInfo.SetAsOffScreen(IntPtr.Zero);
            windowInfo.Width = (int)browserConfig.BrowserSourceSettings.Width;
            windowInfo.Height = (int)browserConfig.BrowserSourceSettings.Height;
            windowInfo.MenuHandle = IntPtr.Zero;
            windowInfo.ParentHandle = IntPtr.Zero;

            String base64EncodedDataUri = "data:text/css;charset=utf-8;base64,";
            String base64EncodedCss = Convert.ToBase64String(Encoding.UTF8.GetBytes(browserConfig.BrowserSourceSettings.CSS));
            
            BrowserInstanceSettings settings = AbstractSettings.DeepClone(BrowserSettings.Instance.InstanceSettings);
            settings.MergeWith(browserConfig.BrowserInstanceSettings);
            
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

            String url = browserConfig.BrowserSourceSettings.Url;

            if (browserConfig.BrowserSourceSettings.IsApplyingTemplate)
            {
                String resolvedTemplate = browserConfig.BrowserSourceSettings.Template;
                resolvedTemplate = resolvedTemplate.Replace("$(FILE)", browserConfig.BrowserSourceSettings.Url);
                resolvedTemplate = resolvedTemplate.Replace("$(WIDTH)", browserConfig.BrowserSourceSettings.Width.ToString());
                resolvedTemplate = resolvedTemplate.Replace("$(HEIGHT)", browserConfig.BrowserSourceSettings.Height.ToString());

                url = "http://absolute";
            }

            // must be sync invoke because wrapper can be destroyed before it is run

            try
            {
                // Since the event methods can be called before the next statement
                // set the status before we call it
                Status = BrowserStatus.Creating;
                CefBrowserHost.CreateBrowser(windowInfo, browserClient, browserSettings, url);
            }
            catch (InvalidOperationException e)
            {
                API.Instance.Log("BrowserWrapper::CreateBrowser failed; {0}", e.Message);
                UninitClient();
                return false;
            }

            BrowserManager.Instance.IncrementBrowserInstanceCount();

            return true;
        }

        private void DoCloseBrowser(bool isForcingClose)
        {

        }
        public void CloseBrowser(bool isForcingClose)
        {
            // the renderer doesn't need to communicate with the browser source
            // after it has been closed
            // this avoids a problem where the browser source gets disposed before it 
            // has completed it's shutdown sequence
            browserClient.RenderHandler.Cleanup();

            // Did we get a close before we finished creating?
            if (Status == BrowserStatus.Creating)
            {
                hasPendingClose = true;
            }
            else if (Status == BrowserStatus.Created)
            {
                Debug.Assert(browser != null);
                Status = BrowserStatus.Closing;
                BrowserManager.Instance.Dispatcher.Invoke(new Action(() => { browser.GetHost().CloseBrowser(isForcingClose); }));
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

        #region Events

        // AfterCreated event
        public void AfterCreated(CefBrowser browser)
        {

            Debug.Assert(this.browser == null);
            Debug.Assert(browser != null);

            this.browser = browser;

            BrowserManager.Instance.RegisterBrowser(browser.Identifier, this);

            Debug.Assert(Status == BrowserStatus.Creating);

            Status = BrowserStatus.Created;

            if (hasPendingClose)
            {
                hasPendingClose = false;
                CloseBrowser(true);
            }
        }

        // DoClose event
        public bool DoClose(CefBrowser browser)
        {
            Debug.Assert(browser != null);

            browser.GetHost().ParentWindowWillClose();

            Debug.Assert(Status == BrowserStatus.Created || Status == BrowserStatus.Closing);

            Status = BrowserStatus.Closing;

            return false;
        }

        // OnBeforeClose event
        public void OnBeforeClose(CefBrowser browser)
        {
            Debug.Assert(Status == BrowserStatus.Closing);
            Debug.Assert(browser != null);

            BrowserManager.Instance.UnregisterBrowser(browser.Identifier);

            browser = null;

            BrowserManager.Instance.DecrementBrowserInstanceCount();

            Status = BrowserStatus.Closed;

            UninitClient();
        }

        #endregion

        #region Properties

        #endregion
    }
}
