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

    internal class BrowserRenderHandler : CefRenderHandler
    {
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
            if (SizeEvent != null)
            {
                return SizeEvent(ref rect);
            }
            return false;
        }

        protected override bool GetViewRect(CefBrowser browser, ref CefRectangle rect)
        {
            if (SizeEvent != null)
            {
                return SizeEvent(ref rect);
            }
            return false;
        }

        protected override bool GetScreenInfo(CefBrowser browser, CefScreenInfo screenInfo)
        {
            if (screenInfo != null)
            {
                return false;
            }
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
            if (CreateTextureEvent != null)
            {
                lock (texturesLock)
                {
                    SharedTexture textureToRender;
                    if (textures.Count <= currentTextureIndex)
                    {
                        IntPtr sharedTextureHandle;
                        CreateTextureEvent((UInt32)width, (UInt32)height, out sharedTextureHandle);

                        if (sharedTextureHandle == IntPtr.Zero)
                        {
                            //texture has not been created yet, try again on the next paint
                            return;
                        }

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
        }

        protected override void OnCursorChange(CefBrowser browser, IntPtr cursorHandle)
        {
        }
        
        protected override void OnScrollOffsetChanged(CefBrowser browser)
        {
        }
        
        public void Cleanup()
        {
            SizeEvent = null;
            
            // Everything but size event must changed under lock while CEF has pending paint commands
            lock (texturesLock)
            {
                PaintEvent = null;
                CreateTextureEvent = null;
                foreach (SharedTexture sharedTexture in textures)
                {
                    DestroyTextureEvent(sharedTexture.Handle);
                }
                textures.Clear();
                
                DestroyTextureEvent = null;
            }
        }

        public SizeEventHandler SizeEvent { private get; set; }
        public PaintEventHandler PaintEvent { private get; set; }
        public CreateTextureEventHandler CreateTextureEvent { private get; set; }
        public DestroyTextureEventHandler DestroyTextureEvent { private get; set; }
    }
}
