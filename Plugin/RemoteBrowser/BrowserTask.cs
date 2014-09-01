using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xilium.CefGlue;

namespace CLRBrowserSourcePlugin.Browser
{
    internal class BrowserTask : CefTask
    {
        private Action task;

        public static CefTask Create(Action task)
        {
            return new BrowserTask(task);
        }

        private BrowserTask(Action task)
        {
            this.task = task;
        }

        protected override void Execute()
        {
            task();
        }
    }
}