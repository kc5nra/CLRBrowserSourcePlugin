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
using System.Windows;
using System.Diagnostics;

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

        internal BrowserPluginManager PluginManager { get; private set; }

        private Thread dispatcherThread;
        private Dispatcher dispatcher;

        private Object browserMapLock = new Object();
        private Dictionary<int, BrowserWrapper> browserMap;

        private bool isMultiThreadedMessageLoop;

        private long browserInstanceCount;  

        public BrowserManager()
        {
            browserMap = new Dictionary<int, BrowserWrapper>();

            ManualResetEvent dispatcherReadyEvent = new ManualResetEvent(false);
            dispatcherThread = new Thread(new ThreadStart(() =>
            {
                dispatcher = Dispatcher.CurrentDispatcher;
                dispatcherReadyEvent.Set();
                Dispatcher.Run();
            }));
            dispatcherThread.Start();

            dispatcherReadyEvent.WaitOne();

            PluginManager = new BrowserPluginManager();
        }

        public void Start()
        {
            ManualResetEventSlim disposedEvent = new ManualResetEventSlim();

            dispatcher.Invoke(new Action(() =>
            {
                CefRuntime.Load();
                CefMainArgs cefMainArgs = new CefMainArgs(IntPtr.Zero, new String[0]);
                BrowserRuntimeSettings settings = BrowserSettings.Instance.RuntimeSettings;

                isMultiThreadedMessageLoop = settings.MultiThreadedMessageLoop;

                CefSettings cefSettings = new CefSettings
                {
                    BrowserSubprocessPath = @"plugins\CLRHostPlugin\CLRBrowserSourcePlugin\CLRBrowserSourcePipe.exe",
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
                
                CefRuntime.RegisterSchemeHandlerFactory("local", null, new AssetSchemeHandlerFactory());
                CefRuntime.RegisterSchemeHandlerFactory("http", "absolute", new AssetSchemeHandlerFactory());
            }));

            PluginManager.Initialize();

        }

        public void Stop()
        {

            int maximumBrowserKillWaitTime = BrowserSettings.Instance.RuntimeSettings.MaximumBrowserKillWaitTime;

            bool isDoingMessageLoopWork = true;

            Thread shutdownThread = new Thread(new ThreadStart(() =>
            {
                while (BrowserInstanceCount > 0)
                {
                    if (!isMultiThreadedMessageLoop)
                    {
                        dispatcher.BeginInvoke(new Action(() => { if (isDoingMessageLoopWork) CefRuntime.DoMessageLoopWork(); }));
                    }

                    GC.Collect(GC.MaxGeneration);
                    GC.WaitForPendingFinalizers();
                    Thread.Sleep(100);
                }
            }));

            shutdownThread.Start();
                       
            while (!shutdownThread.Join(maximumBrowserKillWaitTime))
            {
                MessageBoxResult result = MessageBox.Show("Would you like to continue waiting? \r\nNo will forcefully abort the clients and may result in unexpected behavior.", "Shutting down the browser instances is taking longer than usual.", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                {
                    shutdownThread.Abort();
                    API.Instance.Log("BrowserManager::Stop() Aborting shutdown thread due to timeout.");
                }
            }

            isDoingMessageLoopWork = false;

            if (BrowserInstanceCount > 0)
            {
                API.Instance.Log("BrowserManager::Stop() Unable to dispose of {0} orphaned browser objects", BrowserInstanceCount);
            }

            dispatcher.BeginInvoke(new Action(() => { CefRuntime.Shutdown(); }));
            dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
            if (!dispatcherThread.Join(maximumBrowserKillWaitTime))
            {
                dispatcherThread.Abort();
                API.Instance.Log("BrowserManager::Stop() Unable to abort dispatcher thread, giving up");
            }
        }

        public void Update()
        {
            if (!isMultiThreadedMessageLoop)
            {
                dispatcher.BeginInvoke(new Action(() =>
                {
                    CefRuntime.DoMessageLoopWork();
                }));
            }
        }


        public Dispatcher Dispatcher
        {
            get
            {
                return dispatcher;
            }
        }

        public void IncrementBrowserInstanceCount()
        {
            System.Threading.Interlocked.Increment(ref browserInstanceCount);
        }

        public void DecrementBrowserInstanceCount()
        {
            System.Threading.Interlocked.Decrement(ref browserInstanceCount);
        }

        public long BrowserInstanceCount
        {
            get
            {
                return System.Threading.Interlocked.Read(ref browserInstanceCount);
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
