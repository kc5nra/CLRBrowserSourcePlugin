using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using CLROBS;
using CLRBrowserSourcePlugin.Shared;

using Xilium.CefGlue;
using System.Windows.Threading;
using CLRBrowserSourcePlugin.RemoteBrowser;

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

        #endregion

        private Thread dispatcherThread;
        private Object browserTasksLock = new Object();
        private Dispatcher dispatcher;

        private Object browserMapLock = new Object();
        private Dictionary<int, BrowserConfig> browserMap;

        private bool isMultiThreadedMessageLoop;

        public BrowserManager()
        {
            browserMap = new Dictionary<int, BrowserConfig>();

            ManualResetEvent dispatcherReadyEvent = new ManualResetEvent(false);
            dispatcherThread = new Thread(new ThreadStart(() =>
            {
                dispatcher = Dispatcher.CurrentDispatcher;
                dispatcherReadyEvent.Set();
                Dispatcher.Run();
            }));
            dispatcherThread.Start();

            dispatcherReadyEvent.WaitOne();
        }

        public void Start()
        {
            dispatcher.Invoke(() =>
            {
                CefRuntime.Load();
                CefMainArgs cefMainArgs = new CefMainArgs(IntPtr.Zero, new String[0]);
                BrowserRuntimeSettings settings = BrowserSettings.Instance.RuntimeSettings;

                isMultiThreadedMessageLoop = settings.MultiThreadedMessageLoop;

                CefSettings cefSettings = new CefSettings
                {
                    BrowserSubprocessPath = @"plugins\CLRHostPlugin\CLRBrowserSourcePlugin\CLRBrowserSourceClient.exe",
                    CachePath = settings.CachePath,
                    CommandLineArgsDisabled = settings.CommandLineArgsDisabled,
                    IgnoreCertificateErrors = settings.IgnoreCertificateErrors,
                    JavaScriptFlags = settings.JavaScriptFlags,
                    Locale = settings.Locale,
                    LocalesDirPath = settings.LocalesDirPath,
                    LogFile = settings.LogFile,
                    LogSeverity = settings.LogSeverity,
                    MultiThreadedMessageLoop = settings.MultiThreadedMessageLoop,
                    PersistSessionCookies = settings.PersistSessionCookies,
                    ProductVersion = settings.ProductVersion,
                    ReleaseDCheckEnabled = settings.ReleaseDCheckEnabled,
                    RemoteDebuggingPort = settings.RemoteDebuggingPort,
                    ResourcesDirPath = settings.ResourcesDirPath,
                    SingleProcess = settings.SingleProcess,
                    UncaughtExceptionStackSize = settings.UncaughtExceptionStackSize
                };

                BrowserApp browserApp = new BrowserApp(settings.CommandLineArgsDisabled ? new String[0] : settings.CommandLineArguments);

                CefRuntime.ExecuteProcess(cefMainArgs, browserApp);
                CefRuntime.Initialize(cefMainArgs, cefSettings, browserApp);

                CefRuntime.RefreshWebPlugins();
                
                CefRuntime.RegisterSchemeHandlerFactory("local", null, new AssetSchemeHandlerFactory());
            });
        }

        public void Stop()
        {
            dispatcher.Invoke(() =>
            {
                CefRuntime.ClearSchemeHandlerFactories();
                CefRuntime.Shutdown();
            });

            dispatcher.InvokeShutdown();
            if (!dispatcherThread.Join(500))
            {
                dispatcherThread.Abort();
            }
        }

        public void Update()
        {
            if (!isMultiThreadedMessageLoop)
            {
                dispatcher.InvokeAsync(() =>
                {
                    CefRuntime.DoMessageLoopWork();
                });
            }
        }


        public Dispatcher Dispatcher
        {
            get
            {
                return dispatcher;
            }
        }

        public void RegisterBrowser(int id, BrowserConfig config) 
        {
            lock (browserMapLock)
            {
                browserMap.Add(id, config);
            }
        }

        public bool TryGetBrowserConfig(int id, out BrowserConfig config)
        {
            lock (browserMapLock)
            {
                return browserMap.TryGetValue(id, out config);
            }
        }

        public void UnregisterBrowser(int id)
        {
            lock (browserMapLock)
            {
                browserMap.Remove(id);
            }
        }
    }
}
