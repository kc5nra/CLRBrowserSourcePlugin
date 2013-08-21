using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using CLROBS;
using Xilium.CefGlue;
using System.Windows;

namespace CLRBrowserSourcePlugin.Browser
{
    public delegate bool SizeEventHandler(ref CefRectangle rect);
    public delegate void PaintEventHandler(IntPtr sharedHandle);
    public delegate void CreateTextureEventHandler(UInt32 width, UInt32 height, out IntPtr textureHandle);
    public delegate void DestroyTextureEventHandler(IntPtr textureHandle);

    internal class BrowserRenderHandler : CefRenderHandler, IDisposable
    {
        private bool isDisposed;

        private Object texturesLock = new Object();
        private List<SharedTexture> textures;
        private int currentTextureIndex;
        private int textureCount;

        public BrowserRenderHandler()
        {
            this.textures = new List<SharedTexture>();
            textureCount = 2;
            currentTextureIndex = 0;
        }

        protected override bool GetRootScreenRect(CefBrowser browser, ref CefRectangle rect)
        {
            return SizeEvent(ref rect);
        }

        protected override bool GetViewRect(CefBrowser browser, ref CefRectangle rect)
        {
            return SizeEvent(ref rect);
        }

        protected override bool GetScreenInfo(CefBrowser browser, CefScreenInfo screenInfo)
        {
            return false;
        }

        protected override void OnPopupShow(CefBrowser browser, bool show)
        {
        }

        protected override void OnPopupSize(CefBrowser browser, CefRectangle rect)
        {
        }

        protected override void OnPaint(CefBrowser browser, CefPaintElementType type, CefRectangle[] dirtyRects, IntPtr buffer, int width, int height)
        {
            if (isDisposed)
            {
                return;
            }

            lock (texturesLock)
            {
                SharedTexture textureToRender;
                if (textures.Count <= currentTextureIndex)
                {
                    IntPtr sharedTextureHandle;
                    CreateTextureEvent((UInt32)width, (UInt32)height, out sharedTextureHandle);

                    // TODO : eventually switch to shared textures
                    //Texture texture = GraphicsSystem.Instance.CreateTextureFromSharedHandle((UInt32)width, (UInt32)height, sharedTextureHandle);

                    Texture texture = new Texture(sharedTextureHandle);

                    textureToRender = new SharedTexture
                    {
                        Texture = texture,
                        Handle = sharedTextureHandle
                    };

                    textures.Add(textureToRender);
                }
                else
                {
                    textureToRender = textures[currentTextureIndex];
                }

                textureToRender.Texture.SetImage(buffer, GSImageFormat.GS_IMAGEFORMAT_BGRA, (UInt32)(width * 4));

                // loop the current texture index
                currentTextureIndex = ++currentTextureIndex % textureCount;
                PaintEvent(textureToRender.Handle);
            }
        }

        protected override void OnCursorChange(CefBrowser browser, IntPtr cursorHandle)
        {
        }

        #region Disposable

        ~BrowserRenderHandler()
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
                lock (texturesLock)
                {
                    foreach (SharedTexture sharedTexture in textures)
                    {
                        DestroyTextureEvent(sharedTexture.Handle);
                    }
                    textures.Clear();
                }
            }

            isDisposed = true;
        }

        #endregion

        public SizeEventHandler SizeEvent { private get; set; }
        public PaintEventHandler PaintEvent { private get; set; }
        public CreateTextureEventHandler CreateTextureEvent { private get; set; }
        public DestroyTextureEventHandler DestroyTextureEvent { private get; set; }
    }
}
