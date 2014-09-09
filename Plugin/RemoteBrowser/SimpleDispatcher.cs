using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CLRBrowserSourcePlugin.RemoteBrowser
{
    public class SimpleDispatcher
    {
        private BlockingCollection<Action> dispatchQueue;
        private bool isShutdown;
        private Thread dispatchThread;
        private CancellationTokenSource cts;

        public SimpleDispatcher()
        {
            dispatchQueue = new BlockingCollection<Action>();
            cts = new CancellationTokenSource();
        }

        public void Start()
        {
            ManualResetEventSlim dispatcherStarted =
                new ManualResetEventSlim();
            dispatchThread = new Thread(() =>
            {
                dispatcherStarted.Set();
                while (!isShutdown)
                {
                    var action = dispatchQueue.Take();
                    action();
                }
            });

            dispatchThread.Start();

            dispatcherStarted.Wait();
        }

        public void Shutdown()
        {
            ManualResetEventSlim dispatcherStopped =
                new ManualResetEventSlim();
            dispatchQueue.Add(() =>
            {
                isShutdown = true;
                dispatcherStopped.Set();
            });
            cts.Cancel();
            dispatcherStopped.Wait();
        }

        public void PostTask(Action action)
        {
            dispatchQueue.Add(action);
        }
    }
}