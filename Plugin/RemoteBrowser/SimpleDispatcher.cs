using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
namespace CLRBrowserSourcePlugin.RemoteBrowser
{
    public class SimpleDispatcher
    {

        private BlockingCollection<Action> dispatchQueue;
        private bool isShutdown;

        public SimpleDispatcher()
        {
            dispatchQueue = new BlockingCollection<Action>();
        }

        public void Start()
        {
            ManualResetEventSlim dispatcherStarted =
                new ManualResetEventSlim();
            Task.Factory.StartNew(() =>
            {
                dispatcherStarted.Set();
                while (!isShutdown)
                {
                    var action = dispatchQueue.Take();
                    action();
                }
            });

            dispatcherStarted.Wait();
        }

        public void Shutdown()
        {
            ManualResetEventSlim dispatcherStopped =
                new ManualResetEventSlim();
            dispatchQueue.Add(() =>
            {
                isShutdown = false;
                dispatcherStopped.Set();
            });

            dispatcherStopped.Wait();
        }

        public void PostTask(Action action)
        {
            dispatchQueue.Add(action);
        }
    }
}
