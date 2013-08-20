using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;

using CLROBS;
using CLRBrowserSourcePlugin.Browser;
using System.Reflection;


namespace CLRBrowserSourcePlugin
{
    public class CLRBrowserSourcePlugin : AbstractPlugin
    {
        [DllImport("kernel32")]
        public extern static int LoadLibrary(string librayName);

        public CLRBrowserSourcePlugin()
        {
            Name = "CLR Browser Source Plugin";
            Description = "CLR Browser Source Plugin based on CEF";
        }
        
        public override bool LoadPlugin()
        {
            String currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            String libraryDirectory = Path.Combine(currentDirectory, "CLRBrowserSourcePlugin");

            LoadLibrary(Path.Combine(libraryDirectory, "d3dcompiler_43.dll"));
            LoadLibrary(Path.Combine(libraryDirectory, "d3dcompiler_46.dll"));
            LoadLibrary(Path.Combine(libraryDirectory, "libGLESv2.dll"));
            LoadLibrary(Path.Combine(libraryDirectory, "libEGL.dll"));
            LoadLibrary(Path.Combine(libraryDirectory, "ffmpegsumo.dll"));
            LoadLibrary(Path.Combine(libraryDirectory, "icudt.dll"));
            LoadLibrary(Path.Combine(libraryDirectory, "libcef.dll"));
          
            Browser.BrowserManager.Instance.Start();

            API.Instance.AddImageSourceFactory(new BrowserSourceFactory());
            return true;
        }

        public override void UnloadPlugin()
        {
            BrowserManager.Instance.Stop();
        }

    }
}
