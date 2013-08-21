using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CLROBS;

using CLRBrowserSourcePlugin.Browser;
using CLRBrowserSourcePlugin.Shared;

namespace CLRBrowserSourcePlugin
{
    class BrowserSource : AbstractImageSource, IDisposable
    {
        private bool isDisposed;
        private XElement configElement;
        private BrowserConfig config;

        private BrowserWrapper browser;

        private Dictionary<IntPtr, Texture> textureMap;
        private Object textureLock = new Object();
        private Texture texture;
        private UInt32 outputColor;

        public BrowserSource(XElement configElement)
        {
            this.configElement = configElement;
            this.config = new BrowserConfig();
            this.textureMap = new Dictionary<IntPtr, Texture>();

            UpdateSettings();
        }

        #region Disposable

        ~BrowserSource()
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
                if (browser != null)
                {
                    browser.Dispose();
                    browser = null;
                }
            }

            isDisposed = true;
        }

        #endregion

        override public void UpdateSettings()
        {
            config.Reload(configElement);

            // unscaled w/h
            Size.X = (float)config.BrowserSourceSettings.Width;
            Size.Y = (float)config.BrowserSourceSettings.Height;

            outputColor = 0xFFFFFF | (((uint)(config.BrowserSourceSettings.Opacity * 255) & 0xFF) << 24);

            if (browser != null)
            {
                browser.Dispose();
                browser = null;
            }

            browser = new BrowserWrapper(this);
            browser.UpdateSettings(config);
        }

        public void RenderTexture(IntPtr textureHandle)
        {
            lock (textureLock)
            {
                Texture textureToRender;
                if (textureMap.TryGetValue(textureHandle, out textureToRender))
                {
                    texture = textureToRender;
                }
            }
        }

        public void CreateTexture(UInt32 width, UInt32 height, out IntPtr textureHandle)
        {
            // TODO : switch to shared textures when we go multiprocess
            //textureHandle = GS.CreateSharedTexture(width, height, GSColorFormat.GS_BGRA);
            Texture texture = GS.CreateTexture(width, height, GSColorFormat.GS_BGRA, null, false, false);
            textureMap.Add(texture.OBSTexture, texture);
            textureHandle = texture.OBSTexture;
        }

        public void DestroyTexture(IntPtr textureHandle)
        {
            lock (textureLock)
            {
                Texture textureToRemove;
                if (textureMap.TryGetValue(textureHandle, out textureToRemove))
                {
                    if (texture == textureToRemove) {
                        texture = null;
                    }
                    textureToRemove.Dispose();
                    textureMap.Remove(textureHandle);
                }
            }
            
        }

        override public void Render(float x, float y, float width, float height)
        {
            lock (textureLock)
            {
                if (texture != null)
                {
                    GS.DrawSprite(texture, outputColor, x, y, x + width, y + height);
                }
            }
        }


    }
}
