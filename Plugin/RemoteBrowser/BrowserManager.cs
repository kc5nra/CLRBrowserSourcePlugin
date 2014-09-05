using CLRBrowserSourcePlugin.RemoteBrowser;
using CLRBrowserSourcePlugin.Shared;
using CLROBS;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Xilium.CefGlue;

namespace CLRBrowserSourcePlugin.Browser
{
    internal sealed class BrowserManager
    {
        #region Singleton

        private static BrowserManager instance;

        public static BrowserManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new BrowserManager();
                }

                return instance;
            }
        }

        #endregion Singleton

        internal BrowserPluginManager PluginManager { get; private set; }

        private SimpleDispatcher dispatcher;

        private Object browserMapLock = new Object();
        private Dictionary<int, BrowserWrapper> browserMap;

        private bool isMultiThreadedMessageLoop;
        private bool isSingleProcess;

        public BrowserManager()
        {
            browserMap = new Dictionary<int, BrowserWrapper>();

            dispatcher = new SimpleDispatcher();
            dispatcher.Start();

            PluginManager = new BrowserPluginManager();
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

        public void Start()
        {
            dispatcher.PostTask(new Action(() =>
            {
                CefRuntime.Load();
                CefMainArgs cefMainArgs = new CefMainArgs(IntPtr.Zero, new String[0]);
                BrowserRuntimeSettings settings = BrowserSettings.Instance.RuntimeSettings;

                isMultiThreadedMessageLoop = settings.MultiThreadedMessageLoop;
                isSingleProcess = BrowserSettings.Instance.RuntimeSettings.SingleProcess;

                string browserSubprocessPath = Path.Combine(AssemblyDirectory,
                    "CLRBrowserSourcePlugin", "CLRBrowserSourceClient.exe");

                CefSettings cefSettings = new CefSettings
                {
                    BrowserSubprocessPath = browserSubprocessPath,
                    CachePath = settings.CachePath,
                    CommandLineArgsDisabled = settings.CommandLineArgsDisabled,
                    IgnoreCertificateErrors = settings.IgnoreCertificateErrors,
                    JavaScriptFlags = settings.JavaScriptFlags,
                    Locale = settings.Locale,
                    LocalesDirPath = settings.LocalesDirPath,
                    LogFile = settings.LogFile,
                    LogSeverity = settings.LogSeverity,
                    MultiThreadedMessageLoop = settings.MultiThreadedMessageLoop,
                    NoSandbox = true,
                    PersistSessionCookies = settings.PersistSessionCookies,
                    ProductVersion = settings.ProductVersion,
                    RemoteDebuggingPort = settings.RemoteDebuggingPort,
                    ResourcesDirPath = settings.ResourcesDirPath,
                    SingleProcess = false,
                    UncaughtExceptionStackSize = settings.UncaughtExceptionStackSize,
                    WindowlessRenderingEnabled = true
                };

                BrowserApp browserApp = new BrowserApp(settings.CommandLineArgsDisabled ? new String[0] : settings.CommandLineArguments);

                CefRuntime.ExecuteProcess(cefMainArgs, browserApp, IntPtr.Zero);
                CefRuntime.Initialize(cefMainArgs, cefSettings, browserApp, IntPtr.Zero);

                CefRuntime.RegisterSchemeHandlerFactory("local", null, new AssetSchemeHandlerFactory());
                CefRuntime.RegisterSchemeHandlerFactory("http", "absolute", new AssetSchemeHandlerFactory());

                if (!settings.MultiThreadedMessageLoop)
                {
                    CefRuntime.RunMessageLoop();
                }
            }));
        }

        public void Stop()
        {
            CefRuntime.PostTask(CefThreadId.UI, BrowserTask.Create(() =>
            {
                if (!isMultiThreadedMessageLoop)
                {
                    CefRuntime.QuitMessageLoop();
                }
            }));

            dispatcher.PostTask(() =>
            {
                CefRuntime.Shutdown();
            });

            dispatcher.Shutdown();

            int browserMapCount = browserMap.Count;
            if (browserMapCount != 0)
            {
                throw new CefRuntimeException(String.Format(
                    "After shutting down {0} browser instances were undisposed",
                    browserMapCount));
            }

            dispatcher = null;
        }

        public void Update()
        {
            if (!isMultiThreadedMessageLoop)
            {
                dispatcher.PostTask(new Action(() =>
                {
                    CefRuntime.DoMessageLoopWork();
                }));
            }
        }

        public void RegisterBrowser(int browserIdentifier, BrowserWrapper browserWrapper)
        {
            lock (browserMapLock)
            {
                browserMap.Add(browserIdentifier, browserWrapper);
            }
        }

        public bool TryGetBrowser(int browserIdentifier, out BrowserWrapper browserWrapper)
        {
            lock (browserMapLock)
            {
                return browserMap.TryGetValue(browserIdentifier, out browserWrapper);
            }
        }

        public void UnregisterBrowser(int browserIdentifier)
        {
            lock (browserMapLock)
            {
                browserMap.Remove(browserIdentifier);
            }
        }
    }
}