using CLRBrowserSourcePlugin.Browser;
using CLRBrowserSourcePlugin.Shared;
using CLROBS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLRBrowserSourcePlugin
{
    internal class BrowserSource : AbstractImageSource, IDisposable
    {
        private XElement configElement;
        private BrowserConfig browserConfig;

        private BrowserWrapper browser;

        private Dictionary<IntPtr, Texture> textureMap;
        private Object textureLock = new Object();
        private Texture texture;
        private UInt32 outputColor;

        private bool hasBrowser;

        public BrowserSource(XElement configElement)
        {
            this.configElement = configElement;
            this.browserConfig = new BrowserConfig();
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
                // remove any pending texture requests
                lock (pendingTextureLock)
                {
                    if (pendingTexture != null && pendingTexture.Texture != null)
                    {
                        pendingTexture.Texture.Dispose();
                    }
                    pendingTexture = null;
                }
                if (browser != null)
                {
                    browser.CloseBrowser(true);
                    browser = null;
                }
            }
        }

        #endregion Disposable

        override public void UpdateSettings()
        {
            browserConfig.Reload(configElement);

            // unscaled w/h
            Size.X = (float)browserConfig.BrowserSourceSettings.Width;
            Size.Y = (float)browserConfig.BrowserSourceSettings.Height;

            outputColor = 0xFFFFFF | (((uint)(browserConfig.BrowserSourceSettings.Opacity * 255) & 0xFF) << 24);

            if (hasBrowser || browser == null)
            {
                if (browser != null)
                {
                    browser.CloseBrowser(true);
                }
                browser = new BrowserWrapper();
            }

            hasBrowser = browser.CreateBrowser(this, browserConfig);
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

        public class TextureDesc
        {
            public UInt32 Width { get; set; }

            public UInt32 Height { get; set; }

            public Texture Texture { get; set; }
        }

        private TextureDesc pendingTexture;
        private Object pendingTextureLock = new Object();

        public void CreateTexture(UInt32 width, UInt32 height, out IntPtr textureHandle)
        {
            // TODO : switch to shared textures when we go multiprocess
            //textureHandle = GS.CreateSharedTexture(width, height, GSColorFormat.GS_BGRA);
            lock (pendingTextureLock)
            {
                if (pendingTexture != null && pendingTexture.Width == width && pendingTexture.Height == height)
                {
                    if (pendingTexture.Texture != null)
                    {
                        textureMap.Add(pendingTexture.Texture.OBSTexture, pendingTexture.Texture);
                        textureHandle = pendingTexture.Texture.OBSTexture;
                        pendingTexture = null;
                        return;
                    }
                    else
                    {
                        textureHandle = IntPtr.Zero;
                        return;
                    }
                }
                else
                {
                    if (pendingTexture != null)
                    {
                        // if we have a pending texture that was the wrong size, dispose
                        if (pendingTexture.Texture != null)
                        {
                            pendingTexture.Texture.Dispose();
                        }
                    }

                    pendingTexture = new TextureDesc
                    {
                        Width = width,
                        Height = height
                    };
                    textureHandle = IntPtr.Zero;

                    return;
                }
            }
        }

        public void DestroyTexture(IntPtr textureHandle)
        {
            lock (textureLock)
            {
                Texture textureToRemove;
                if (textureMap.TryGetValue(textureHandle, out textureToRemove))
                {
                    if (texture == textureToRemove)
                    {
                        texture = null;
                    }
                    textureToRemove.Dispose();
                    textureMap.Remove(textureHandle);
                }
            }
        }

        public override void Preprocess()
        {
            // only does something if browser is single threaded event loop
            BrowserManager.Instance.Update();

            if (!hasBrowser)
            {
                UpdateSettings();
            }

            lock (pendingTextureLock)
            {
                if (pendingTexture != null && pendingTexture.Texture == null)
                {
                    pendingTexture.Texture = GS.CreateTexture(pendingTexture.Width, pendingTexture.Height, GSColorFormat.GS_BGRA, null, false, false);
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