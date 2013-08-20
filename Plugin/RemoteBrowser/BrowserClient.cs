using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xilium.CefGlue;

namespace CLRBrowserSourcePlugin.Browser
{
    internal class BrowserClient : CefClient, IDisposable
    {
        private bool isDisposed;

        private BrowserLifeSpanHandler lifeSpanHandler;
        private BrowserDisplayHandler displayHandler;
        private BrowserRenderHandler renderHandler;

        public BrowserClient()
        {
            
            lifeSpanHandler = new BrowserLifeSpanHandler();
            displayHandler = new BrowserDisplayHandler();
            renderHandler = new BrowserRenderHandler();
        }

        protected override CefLifeSpanHandler GetLifeSpanHandler()
        {
            return lifeSpanHandler;
        }

        protected override CefDisplayHandler GetDisplayHandler()
        {
            return displayHandler;
        }

        protected override CefRenderHandler GetRenderHandler()
        {
            return renderHandler;
        }

        #region Disposable

        ~BrowserClient()
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
                if (renderHandler != null)
                {
                    renderHandler.Dispose();
                    renderHandler = null;
                }
            }

            isDisposed = true;
        }

        #endregion

        #region Properties

        public BrowserLifeSpanHandler LifeSpanHandler
        {
            get
            {
                return lifeSpanHandler;
            }
        }

        public BrowserDisplayHandler DisplayHandler
        {
            get
            {
                return displayHandler;
            }
        }

        public BrowserRenderHandler RenderHandler
        {
            get
            {
                return renderHandler;
            }
        }

        #endregion

    }
}
