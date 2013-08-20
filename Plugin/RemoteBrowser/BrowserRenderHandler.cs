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
        private SizeEventHandler sizeEventHandler;
        private PaintEventHandler paintEventHandler;
        private CreateTextureEventHandler createTextureEventHandler;
        private DestroyTextureEventHandler destroyTextureEventHandler;

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
            return sizeEventHandler(ref rect);
        }

        protected override bool GetViewRect(CefBrowser browser, ref CefRectangle rect)
        {
            return sizeEventHandler(ref rect);
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
                    createTextureEventHandler((UInt32)width, (UInt32)height, out sharedTextureHandle);

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
                paintEventHandler(textureToRender.Handle);
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
                        destroyTextureEventHandler(sharedTexture.Handle);
                    }
                    textures.Clear();
                }
            }

            isDisposed = true;
        }

        #endregion

        public SizeEventHandler SizeEvent
        {
            set
            {
                sizeEventHandler = value;
            }
        }


        public PaintEventHandler PaintEvent
        {
            set
            {
                paintEventHandler = value;
            }
        }

        public CreateTextureEventHandler CreateTextureEvent
        {
            set
            {
                createTextureEventHandler = value;
            }
        }

        public DestroyTextureEventHandler DestroyTextureEvent
        {
            set
            {
                destroyTextureEventHandler = value;
            }
        }
    }
}
