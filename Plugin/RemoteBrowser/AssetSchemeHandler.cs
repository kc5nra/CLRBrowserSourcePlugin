using CLRBrowserSourcePlugin.Browser;
using CLRBrowserSourcePlugin.Shared;
using CLROBS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xilium.CefGlue;

namespace CLRBrowserSourcePlugin.RemoteBrowser
{
    class AssetSchemeHandlerFactory : CefSchemeHandlerFactory
    {
        protected override CefResourceHandler Create(CefBrowser browser, CefFrame frame, string schemeName, CefRequest request)
        {
            if (browser == null || request == null)
            {
                API.Instance.Log("Browser null - Frame null: requested with null browser or request (rapidly opening and closing?)");
                return null;
            }

            BrowserWrapper browserWrapper;

            if (BrowserManager.Instance.TryGetBrowser(browser.Identifier, out browserWrapper))
            {
                return new AssetSchemeHandler(browserWrapper.BrowserConfig, request);
            }
            else
            {
                API.Instance.Log("Browser {0} - Frame {1}: {2} scheme with request {3} failed to locate browser config; Defaulting", browser.Identifier, (frame != null) ? frame.Identifier : -1, schemeName, request.Url);
                return null;
            }
        }
    }

    class AssetSchemeHandler : CefResourceHandler
    {
        private BrowserConfig config;

        private Uri uri;
        
        private String resolvedPath;
        private Stream inputStream;

        private Boolean isAssetWrapping;

        private bool isComplete;
        private long length;
        private long remaining;

        public AssetSchemeHandler(BrowserConfig config, CefRequest request)
        {
            this.config = config;
            isComplete = false;

            length = remaining = -1;
            
        }

        protected override void GetResponseHeaders(CefResponse response, out long responseLength, out string redirectUrl)
        {
            if (response == null)
            {
                responseLength = -1;
                redirectUrl = null;
                return;
            }

            String extension = Path.GetExtension(uri.LocalPath);
            if (extension.Length > 1 && extension.StartsWith(".")) {
                extension = extension.Substring(1);
            }

            String mimeType;

            if (!MimeTypeManager.MimeTypes.TryGetValue(extension, out mimeType))
            {
                mimeType = "text/html";
            }

            response.Status = 200;
            response.MimeType = mimeType;
            response.StatusText = "OK";
            responseLength = length;
            redirectUrl = null;  // no-redirect
        }

        protected override bool ProcessRequest(CefRequest request, CefCallback callback)
        {
            if (request == null || callback == null)
            {
                return false;
            }
            try
            {
                uri = new Uri(request.Url);
            }
            catch (Exception e)
            {
                API.Instance.Log("AssetSchemeHandler::ProcessRequest: Unable to parse path {0}", request.Url);
                return false;
            }

            if (uri.LocalPath.Length == 1)
            {
                isAssetWrapping = true;
                String resolvedTemplate = config.BrowserSourceSettings.Template;
                resolvedTemplate = resolvedTemplate.Replace("$(FILE)", config.BrowserSourceSettings.Url);
                resolvedTemplate = resolvedTemplate.Replace("$(WIDTH)", config.BrowserSourceSettings.Width.ToString());
                resolvedTemplate = resolvedTemplate.Replace("$(HEIGHT)", config.BrowserSourceSettings.Height.ToString());
                inputStream = new MemoryStream(Encoding.UTF8.GetBytes(resolvedTemplate));
                
            }
            else
            {
                isAssetWrapping = false;

                if (uri.LocalPath != null && uri.LocalPath.Length > 1)
                {
                    resolvedPath = uri.LocalPath.Substring(1);
                }
                else
                {
                    API.Instance.Log("AssetSchemeHandler::ProcessRequest: Unable to parse path {0}", request.Url);
                    return false;
                }
               
                try
                {
                    inputStream = new FileStream(resolvedPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                catch (Exception e)
                {
                    if (inputStream != null)
                    {
                        inputStream.Dispose();
                        inputStream = null;
                    }

                    API.Instance.Log("AssetSchemeHandler::ProcessRequest of file {0} failed; {1}", resolvedPath, e.Message);
                    callback.Cancel();
                    return false;
                }
            }

            length = remaining = inputStream.Length;

            callback.Continue();
            return true;
        }

        protected override bool ReadResponse(Stream response, int bytesToRead, out int bytesRead, CefCallback callback)
        {
            if (response == null || inputStream == null)
            {
                if (inputStream != null)
                {
                    inputStream.Dispose();
                    inputStream = null;
                }
                bytesRead = 0;
                return false;
            }

            try
            {
                if (isComplete)
                {
                    bytesRead = 0;
                    return false;
                }

                bytesRead = StreamUtils.CopyStream(inputStream, response, bytesToRead);
                remaining -= bytesRead;


                //remaining -= bytesRead;

                if (remaining == 0)
                {
                    isComplete = true;
                    inputStream.Dispose();
                    inputStream = null;
                }

                return true;
            }
            catch (Exception e)
            {
                API.Instance.Log("AssetSchemeHandler::ReadResponse of file {0} failed; {1}", isAssetWrapping ? "{wrapped asset}" : resolvedPath, e.Message);
                if (inputStream != null)
                {
                    inputStream.Dispose();
                    inputStream = null;
                }
                bytesRead = 0;
                callback.Cancel();
                return false;
            }
        }

        protected override void Cancel()
        {
            if (inputStream != null)
            {
                inputStream.Close();
                inputStream = null;
            }
        }

        protected override bool CanGetCookie(CefCookie cookie)
        {
            return true;
        }

        protected override bool CanSetCookie(CefCookie cookie)
        {
            return true;
        }
    }
}
