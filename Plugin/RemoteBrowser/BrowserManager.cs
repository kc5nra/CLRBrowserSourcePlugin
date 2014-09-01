using CLRBrowserSourcePlugin.RemoteBrowser;
using CLRBrowserSourcePlugin.Shared;
using CLROBS;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        private Thread dispatcherThread;
        private Dispatcher dispatcher;

        private Object browserMapLock = new Object();
        private Dictionary<int, BrowserWrapper> browserMap;

        private bool isMultiThreadedMessageLoop;
        private bool isSingleProcess;

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

            dispatcher.InvokeAsync(new Action(() =>
            {
                CefRuntime.Load();
                CefMainArgs cefMainArgs = new CefMainArgs(IntPtr.Zero, new String[0]);
                BrowserRuntimeSettings settings = BrowserSettings.Instance.RuntimeSettings;

                isMultiThreadedMessageLoop = settings.MultiThreadedMessageLoop;
                isSingleProcess = BrowserSettings.Instance.RuntimeSettings.SingleProcess;

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
                    MultiThreadedMessageLoop = false,
                    PersistSessionCookies = settings.PersistSessionCookies,
                    ProductVersion = settings.ProductVersion,
                    RemoteDebuggingPort = settings.RemoteDebuggingPort,
                    ResourcesDirPath = settings.ResourcesDirPath,
                    SingleProcess = settings.SingleProcess,
                    UncaughtExceptionStackSize = settings.UncaughtExceptionStackSize,
                    WindowlessRenderingEnabled = true
                };

                BrowserApp browserApp = new BrowserApp(settings.CommandLineArgsDisabled ? new String[0] : settings.CommandLineArguments);

                CefRuntime.ExecuteProcess(cefMainArgs, browserApp, IntPtr.Zero);
                CefRuntime.Initialize(cefMainArgs, cefSettings, browserApp, IntPtr.Zero);

                CefRuntime.RegisterSchemeHandlerFactory("local", null, new AssetSchemeHandlerFactory());
                CefRuntime.RegisterSchemeHandlerFactory("http", "absolute", new AssetSchemeHandlerFactory());
                CefRuntime.RunMessageLoop();
                CefRuntime.Shutdown();
            }));

            //PluginManager.Initialize();
        }

        public void Stop()
        {
            CefRuntime.PostTask(CefThreadId.UI, BrowserTask.Create(() =>
            {
                CefRuntime.QuitMessageLoop();
            }));

            Dispatcher.InvokeShutdown();

            int browserMapCount = browserMap.Count;
            if (browserMapCount != 0)
            {
                throw new CefRuntimeException(String.Format(
                    "After shutting down {0} browser instances were undisposed",
                    browserMapCount));
            }

            dispatcher = null;
            //int maximumBrowserKillWaitTime = BrowserSettings.Instance.RuntimeSettings.MaximumBrowserKillWaitTime;

            //bool isDoingMessageLoopWork = true;

            //Thread shutdownThread = new Thread(new ThreadStart(() =>
            //{
            //    while (BrowserInstanceCount > 0)
            //    {
            //        if (!isMultiThreadedMessageLoop)
            //        {
            //            dispatcher.BeginInvoke(new Action(() => { if (isDoingMessageLoopWork) CefRuntime.DoMessageLoopWork(); }));
            //        }

            //        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            //        GC.WaitForPendingFinalizers();
            //        Thread.Sleep(100);
            //    }
            //}));

            //shutdownThread.Start();

            //while (!shutdownThread.Join(maximumBrowserKillWaitTime))
            //{
            //    MessageBoxResult result = MessageBox.Show("Would you like to continue waiting? \r\nNo will forcefully abort the clients and may result in unexpected behavior.", "Shutting down the browser instances is taking longer than usual.", MessageBoxButton.YesNo);
            //    if (result == MessageBoxResult.No)
            //    {
            //        shutdownThread.Abort();
            //        API.Instance.Log("BrowserManager::Stop() Aborting shutdown thread due to timeout.");
            //    }
            //}

            //isDoingMessageLoopWork = false;

            //if (BrowserInstanceCount > 0)
            //{
            //    API.Instance.Log("BrowserManager::Stop() Unable to dispose of {0} orphaned browser objects", BrowserInstanceCount);
            //}

            //dispatcher.BeginInvoke(new Action(() =>
            //{
            //    PrivateShutdown();
            //}));

            //dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
            //if (!dispatcherThread.Join(maximumBrowserKillWaitTime))
            //{
            //    dispatcherThread.Abort();
            //    API.Instance.Log("BrowserManager::Stop() Unable to abort dispatcher thread, giving up");
            //}
        }

        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        private void PrivateShutdown()
        {
            try
            {
                CefRuntime.Shutdown();
            }
            catch (AccessViolationException e)
            {
                if (isSingleProcess)
                {
                    API.Instance.Log("BrowserManager::Stop() Failed shutting down with exception {0}.  This is a known bug in CEF SingleProcess mode. Try setting SingleProcess to false in Runtime Settings if possible.", e.ToString());
                }
                if (isMultiThreadedMessageLoop)
                {
                    API.Instance.Log("BrowserManager::Stop() Failed shutting down with exception {0}.  This is a known bug in CEF MultiThreadedMessageLoop mode. Try setting MultiThreadedMessageLoop to false in Runtime Settings if possible.", e.ToString());
                }
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