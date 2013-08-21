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

        private IDictionary<String, BrowserSource> browserSources;
        private Thread dispatcherThread;
        private Object browserTasksLock = new Object();
        private Dispatcher dispatcher;

        public BrowserManager()
        {
            browserSources = new Dictionary<String, BrowserSource>();
            
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
                CefSettings cefSettings = new CefSettings
                {
                    SingleProcess = false,
                    MultiThreadedMessageLoop = true,
                    RemoteDebuggingPort = 1337,
                    LogSeverity = CefLogSeverity.Verbose,
                    LogFile = "cef.log",
                    BrowserSubprocessPath = @"plugins\CLRHostPlugin\CLRBrowserSourcePlugin\CLRBrowserSourceClient.exe",
                };

                CefRuntime.ExecuteProcess(cefMainArgs, null);
                CefRuntime.Initialize(cefMainArgs, cefSettings, null);

            });
        }

        public void Stop()
        {
            dispatcher.Invoke(() =>
            {
                CefRuntime.Shutdown();
            });

            dispatcher.InvokeShutdown();
            if (!dispatcherThread.Join(500))
            {
                dispatcherThread.Abort();
            }
        }

        public Dispatcher Dispatcher
        {
            get
            {
                return dispatcher;
            }
        }
    }
}
