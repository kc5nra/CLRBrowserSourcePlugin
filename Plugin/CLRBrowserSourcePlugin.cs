using CLRBrowserSourcePlugin.Browser;
using CLROBS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

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

        public override bool LoadPlugin()
        {
            String libraryDirectory = Path.Combine(AssemblyDirectory, "CLRBrowserSourcePlugin");

            LoadLibrary(Path.Combine(libraryDirectory, "d3dcompiler_43.dll"));
            LoadLibrary(Path.Combine(libraryDirectory, "d3dcompiler_46.dll"));
            LoadLibrary(Path.Combine(libraryDirectory, "libGLESv2.dll"));
            LoadLibrary(Path.Combine(libraryDirectory, "libEGL.dll"));
            LoadLibrary(Path.Combine(libraryDirectory, "ffmpegsumo.dll"));
            LoadLibrary(Path.Combine(libraryDirectory, "icudt.dll"));
            LoadLibrary(Path.Combine(libraryDirectory, "libcef.dll"));

            Browser.BrowserManager.Instance.Start();

            API.Instance.AddImageSourceFactory(new BrowserSourceFactory());
            API.Instance.AddSettingsPane(new BrowserSettingsPane());
            return true;
        }

        public override void UnloadPlugin()
        {
            BrowserManager.Instance.Stop();
            Dispatcher.CurrentDispatcher.InvokeShutdown();
        }
    }
}