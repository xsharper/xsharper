using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RunScript
{
    [Serializable]
    public abstract class JobContext
    {
        private IAsyncResult _asyncResult;
        private Stopwatch _sinceStopped;
        private readonly object _lock=new object();

        protected JobContext()
        {
        }

        protected void Start()
        {
            _asyncResult = new Action(Execute).BeginInvoke(onCompleted, null);
        }

        public bool IsCompleted
        {
            get
            {
                return _asyncResult.IsCompleted;
            }
        }

        public bool IsOld
        {
            get
            {
                lock (_lock)
                {
                    return _sinceStopped != null && _sinceStopped.ElapsedMilliseconds > 1000 * 10 * 60;
                }
            }
        }

        private void onCompleted(IAsyncResult ar)
        {
            lock (_lock)
                _sinceStopped = Stopwatch.StartNew();
        }
        protected abstract void Execute();
    }


    public class JobManager
    {
        private Dictionary<Guid, JobContext> _jobs = new Dictionary<Guid, JobContext>();

        public Guid AddJob(JobContext job)
        {
            Guid g = Guid.NewGuid();
            lock (_jobs)
                _jobs.Add(g, job);
            return g;
        }

        public JobContext FindJob(Guid g)
        {
            lock (_jobs)
            {
                JobContext res;
                return _jobs.TryGetValue(g, out res) ? res : null;
            }
        }
        public T FindJob<T>(Guid g) where T:JobContext
        {
            return FindJob(g) as T;
        }
        public void RemoveJob(Guid g)
        {
            lock (_jobs)
                _jobs.Remove(g);
        }
        public void RemoveOldJobs()
        {
            List<Guid> g = new List<Guid>();
            lock (_jobs)
            {
                foreach (var job in _jobs)
                    if (job.Value.IsOld)
                        g.Add(job.Key);

                foreach (var guid in g)
                    _jobs.Remove(guid);
            }
        }


    }
}