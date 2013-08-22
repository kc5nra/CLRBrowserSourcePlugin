using CLRBrowserSourcePlugin.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xilium.CefGlue;

namespace CLRBrowserSourceClient
{
    class CLRBrowserSourceClient
    {
        [DllImport("kernel32")]
        public extern static int LoadLibrary(string librayName);

        public static int Main(String[] args)
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

            CefMainArgs mainArgs = new CefMainArgs(args);
            return CefRuntime.ExecuteProcess(mainArgs, new BrowserApp());
        }
    }
}
