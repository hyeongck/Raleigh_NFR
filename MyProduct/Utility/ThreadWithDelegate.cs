using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MyProduct
{
    public class ThreadWithDelegate
    {
        public delegate void DoWorkExternal();
        public DoWorkExternal WorkExternal;
        private ManualResetEvent _doneEvent;

        public ThreadWithDelegate(ManualResetEvent doneEvent)
        {
            _doneEvent = doneEvent;
        }

        // Wrapper method for use with thread pool.
        public void ThreadPoolCallback(Object threadContext)
        {
            WorkExternal();
            _doneEvent.Set();
        }
    }
}
