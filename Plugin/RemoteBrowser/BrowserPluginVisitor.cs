using CLRBrowserSourcePlugin.Browser;
using CLRBrowserSourcePlugin.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Xilium.CefGlue;

namespace CLRBrowserSourcePlugin.RemoteBrowser
{

    // Whacky shit to get around the fact that it never calls this visitor if there are no plugins...
    class BrowserPluginVisitor : CefWebPluginInfoVisitor
    {
        private ManualResetEventSlim disposedEvent;
        private Action<CefWebPluginInfo> action;

        public BrowserPluginVisitor(ManualResetEventSlim disposedEvent, Action<CefWebPluginInfo> action)
        {
            this.disposedEvent = disposedEvent;
            this.action = action;
        }

        protected override bool Visit(CefWebPluginInfo pluginInfo, int count, int total)
        {
            action.Invoke(pluginInfo);
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            disposedEvent.Set();
            base.Dispose(disposing);
        }

        public static void Visit(Action<CefWebPluginInfo> action) 
        {
            ManualResetEventSlim disposedEvent = new ManualResetEventSlim();

            // Needs to be run on a different thread or else it will lock up and never release the object
            BrowserManager.Instance.Dispatcher.BeginInvoke(new Action(() =>
            {
                CefRuntime.VisitWebPluginInfo(new BrowserPluginVisitor(disposedEvent, action));
            }));

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            int waitTime = BrowserSettings.Instance.RuntimeSettings.MaximumBrowserKillWaitTime;

            while (!disposedEvent.Wait(100))
            {
                if (stopwatch.ElapsedMilliseconds > waitTime)
                {
                    MessageBox.Show("The browser plugin attempted to enumerate the installed plugins but failed.", "Could not enumerate browser plugins", MessageBoxButton.OK);
                    break;
                }
                BrowserManager.Instance.Update();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }
}
