using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CLROBS;

namespace CLRBrowserSourcePlugin.Browser
{
    public class SharedTexture
    {
        private IntPtr handle;
        private Texture texture;

        public IntPtr Handle
        {
            get
            {
                return handle;
            }
            set
            {
                handle = value;
            }
        }

        public Texture Texture
        {
            get
            {
                return texture;
            }
            set
            {
                texture = value;
            }
        }
    }
}
