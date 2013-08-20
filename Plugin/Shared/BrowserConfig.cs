using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CLROBS;

namespace CLRBrowserSourcePlugin.Shared
{
    class BrowserConfig
    {
        String url;
        UInt32 width;
        UInt32 height;
        String customCss;
        bool isWrappingAsset;
        String assetWrapTemplate;
        bool isExposingOBSApi;

        public void Reload(XElement element)
        {
            url = element.GetString("url");
            width = (UInt32)element.GetInt("width");
            height = (UInt32)element.GetInt("height");
            customCss = element.GetString("css");
            isWrappingAsset = (element.GetInt("isWrappingAsset") == 1);
            assetWrapTemplate = element.GetString("assetWrapTemplate");
            isExposingOBSApi = (element.GetInt("isExposingOBSApi") == 1);
        }

        public void Save(XElement element)
        {
            element.SetString("url", url);
            element.SetInt("width", (int)width);
            element.SetInt("height", (int)height);
            element.SetString("css", customCss);
            element.SetInt("isWrappingAsset", (isWrappingAsset) ? 1 : 0);
            element.SetString("assetWrapTemplate", assetWrapTemplate);
            element.SetInt("isExposingOBSApi", (isExposingOBSApi) ? 1 : 0);
        }

        public void Populate() 
        {
            url = "http://www.apexvj.com/kaksi/player?s=106063334";
            width = 1024;
            height = 768;
            customCss = 
                  "::-webkit-scrollbar {\r\n"
                + "  visibility: hidden;\r\n"
                + "}\r\n"
                + "body {\r\n"
                + "  background-color: rgba(0, 0, 0, 0);\r\n"
                + "  margin: 0px auto;\r\n"
                + "}\r\n";
            isWrappingAsset = false;
            assetWrapTemplate =
                  "<html>\r\n"
                + "  <head>\r\n"
                + " 	  <meta charset='utf-8'/>\r\n"
                + "  </head>\r\n"
                + "  <body>\r\n"
                + "    <object width='$(WIDTH)' height='$(HEIGHT)'>\r\n"
                + "      <param name='movie' value='$(FILE)'></param>\r\n"
                + "      <param name='allowscriptaccess' value='always'></param>\r\n"
                + "      <param name='wmode' value='transparent'></param>\r\n"
                + "      <embed \r\n"
                + "        src='$(FILE)' \r\n"
                + "        type='application/x-shockwave-flash' \r\n"
                + "        allowscriptaccess='always' \r\n"
                + "        width='$(WIDTH)' \r\n"
                + "        height='$(HEIGHT)' \r\n"
                + "        wmode='transparent'>\r\n"
                + "      </embed>\r\n"
                + "    </object>\r\n"
                + "  </body>\r\n"
                + "</html>\r\n";
            isExposingOBSApi = true;
        }

        #region Properties

        public String Url
        {
            get
            {
                return url;
            }

            set
            {
                url = value;
            }
        }

        public UInt32 Width
        {
            get
            {
                return width;
            }

            set
            {
                width = value;
            }
        }

        public UInt32 Height
        {
            get
            {
                return height;
            }

            set
            {
                height = value;
            }
        }

        public String CustomCss
        {
            get
            {
                return customCss;
            }

            set
            {
                customCss = value;
            }
        }

        public bool IsWrappingAsset
        {
            get
            {
                return isWrappingAsset;
            }

            set
            {
                isWrappingAsset = value;
            }
        }

        public String AssetWrapTemplate
        {
            get
            {
                return assetWrapTemplate;
            }

            set
            {
                assetWrapTemplate = value;
            }
        }
       
        public bool IsExposingOBSApi
        {
            get
            {
                return IsExposingOBSApi;
            }

            set
            {
                IsExposingOBSApi = value;
            }
        }

        #endregion
    }
}
