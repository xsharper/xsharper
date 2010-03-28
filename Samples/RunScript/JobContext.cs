using System;
using System.Diagnostics;
using System.Threading;

namespace RunScript
{
    [Serializable]
    public abstract class JobContext : IDisposable
    {
        private Stopwatch _sinceStopped;
        private readonly object _lock=new object();
        private ManualResetEvent _completed=new ManualResetEvent(false);
        private volatile bool _disposeWhenStopped;

        /// Completed task is removed after this period expires
        protected TimeSpan TimeToGetOld=TimeSpan.FromMinutes(10); 

        ~JobContext()
        {
            Dispose(false);
        }

        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool dispose)
        {
            if (_completed != null)
            {
                _completed.Close();
                _completed = null;
            }
        }

        /// Start the job
        public void Start()
        {
            ThreadPool.QueueUserWorkItem((state)=>
                {
                    try
                    {
                        Execute();
                    }
                    finally
                    {
                        lock (_lock)
                        {
                            if (_disposeWhenStopped)
                                Dispose();
                            else
                            {
                                _completed.Set();
                                _sinceStopped = Stopwatch.StartNew();
                            }
                        }
                    }
                });
        }

        /// Dispose the job if it's completed, or mark it to complete when it terminates
        public void DisposeOnCompletion()
        {
            lock (_lock)
            {
                if (IsCompleted)
                    Dispose();
                else
                    _disposeWhenStopped = true;
            }
        }

        /// True, if job is completed
        public bool IsCompleted
        {
            get
            {
                return _completed==null || _completed.WaitOne(0);
            }
        }

        /// True, if job is too old and can be cleaned up
        public bool IsOld
        {
            get
            {
                lock (_lock)
                {
                    return _sinceStopped != null && _sinceStopped.Elapsed > TimeToGetOld;
                }
            }
        }

 
        /// Job
        protected abstract void Execute();
    }
}