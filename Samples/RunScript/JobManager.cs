using System;
using System.Collections.Generic;

namespace RunScript
{
    public class JobManager
    {
        private readonly Dictionary<Guid, JobContext> _jobs = new Dictionary<Guid, JobContext>();

        public Guid Add(JobContext job)
        {
            RemoveOld();
            Guid g = Guid.NewGuid();
            lock (_jobs)
                _jobs.Add(g, job);
            return g;
        }

        public JobContext Find(Guid g)
        {
            lock (_jobs)
            {
                JobContext res;
                return _jobs.TryGetValue(g, out res) ? res : null;
            }
        }
        public T Find<T>(Guid g) where T:JobContext
        {
            return Find(g) as T;
        }
        public void StopAll()
        {
            List<Guid> jids=new List<Guid>();
            lock (_jobs)
                jids.AddRange(_jobs.Keys);
            foreach (var jid in jids)
                Stop(jid);
        }
        
        public void Stop(Guid g)
        {
            JobContext j;
            lock (_jobs)
                j=Find(g);
            if (j != null)
                j.Stop();
        }
        public void Remove(Guid g)
        {
            lock (_jobs)
            {
                var j = Find(g);
                if (j != null)
                    j.DisposeOnCompletion();
                _jobs.Remove(g);
            }
        }
        public void RemoveOld()
        {
            List<Guid> g = new List<Guid>();
            lock (_jobs)
            {
                foreach (var job in _jobs)
                    if (job.Value.IsOld)
                        g.Add(job.Key);

                foreach (var guid in g)
                    Remove(guid);
            }
        }


    }
}