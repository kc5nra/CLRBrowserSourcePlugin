using CLRBrowserSourcePlugin.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Xilium.CefGlue;

namespace CLRBrowserSourceClient
{
    internal class CLRBrowserSourceClient
    {
        [DllImport("kernel32")]
        public extern static int LoadLibrary(string librayName);

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public static int Main(String[] args)
        {
            LoadLibrary(Path.Combine(AssemblyDirectory, "d3dcompiler_43.dll"));
            LoadLibrary(Path.Combine(AssemblyDirectory, "d3dcompiler_46.dll"));
            LoadLibrary(Path.Combine(AssemblyDirectory, "libGLESv2.dll"));
            LoadLibrary(Path.Combine(AssemblyDirectory, "libEGL.dll"));
            LoadLibrary(Path.Combine(AssemblyDirectory, "ffmpegsumo.dll"));
            LoadLibrary(Path.Combine(AssemblyDirectory, "icudt.dll"));
            LoadLibrary(Path.Combine(AssemblyDirectory, "libcef.dll"));

            CefMainArgs mainArgs = new CefMainArgs(IntPtr.Zero, args);
            return CefRuntime.ExecuteProcess(mainArgs, new BrowserApp(), IntPtr.Zero);
        }
    }
}