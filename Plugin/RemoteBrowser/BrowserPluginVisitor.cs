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
    internal class BrowserPluginVisitor : CefWebPluginInfoVisitor
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
            CefRuntime.PostTask(CefThreadId.UI, BrowserTask.Create(() =>
            {
                CefRuntime.VisitWebPluginInfo(new BrowserPluginVisitor(disposedEvent, action));
            }));
        }
    }
}