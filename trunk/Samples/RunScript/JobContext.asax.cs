using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Diagnostics;

namespace RunScript
{
    [Serializable]
    public class JobContext
    {
        private readonly IAsyncResult _asyncResult;
        private readonly StringBuilder _currentHtml = new StringBuilder();
        private readonly XSharper.Core.ScriptContext _context = new XSharper.Core.ScriptContext();
        private Stopwatch _sinceStopped;
        private XSharper.Core.OutputType? _outputType ;

        public JobContext(string scriptText, string args, bool debug)
        {
            _asyncResult = new Action<string, string, bool>(execute).BeginInvoke(scriptText, args, debug, onCompleted, null);
        }

        public bool IsCompleted
        {
            get
            {
                return _asyncResult.IsCompleted;
            }
        }

        public string GetHtmlUpdate()
        {
            lock (_currentHtml)
            {
                string s = _currentHtml.ToString();
                _currentHtml.Length = 0;
                return s;
            }
        }

        
        public bool IsOld
        {
            get
            {
                lock (_currentHtml)
                {
                    return _sinceStopped != null && _sinceStopped.ElapsedMilliseconds > 1000 * 10 * 60;
                }
            }
        }

        #region -- Private stuff --
        private void onCompleted(IAsyncResult ar)
        {
            lock (_currentHtml)
                _sinceStopped = Stopwatch.StartNew();
        }

        private void execute(string scriptText, string args, bool debug)
        {
            using (var xs = new XSharper.Core.ScriptContextScope(_context))
            {
                Stopwatch sw = Stopwatch.StartNew();
                try
                {
                    _context.Clear();
                    _context.Output = output;
                    if (debug)
                        _context.MinOutputType = XSharper.Core.OutputType.Nul;
                    else
                        _context.MinOutputType = XSharper.Core.OutputType.Info;

                    var script = _context.CreateNewScript("temp.xsh");
                    script.Load(scriptText);
                    var ret=_context.ExecuteScript(script, XSharper.Core.Utils.SplitArgs(args));
                    lock (_currentHtml)
                    {
                        _currentHtml.Append("<hr />");
                        _outputType = null;
                    }
                    _context.Info.WriteLine("Execution completed in {0}. Exit code={1}",sw.Elapsed,ret??0);
                }
                catch (Exception ee)
                {
                    lock (_currentHtml)
                    {
                        _currentHtml.Append("<hr />");
                        _outputType = null;
                    }
                    _context.WriteException(ee);
                }
                
            }
        }

        void output(object sender, XSharper.Core.OutputEventArgs eventArgs)
        {
            lock (_currentHtml)
            {
                string text = eventArgs.Text;
                XSharper.Core.OutputType otype = eventArgs.OutputType;
                if (_outputType.HasValue && otype != _outputType)
                {
                    if (!(otype == XSharper.Core.OutputType.Bold && _outputType == XSharper.Core.OutputType.Out) &&
                        !(otype == XSharper.Core.OutputType.Out && _outputType == XSharper.Core.OutputType.Bold))
                    {
                        _currentHtml.Append("<br />");
                    }
                }
                if (!string.IsNullOrEmpty(text))
                {
                    if (otype != XSharper.Core.OutputType.Out)
                        _currentHtml.Append("<span class='" + otype + "'>");

                    _currentHtml.Append(HttpUtility.HtmlEncode(text).Replace("\n", "<br/>"));

                    if (otype != XSharper.Core.OutputType.Out)
                        _currentHtml.Append("</span>");
                }
                _outputType = otype;
            }
        }
        #endregion


        public void Stop()
        {
            _context.Abort();
        }
    }


    public class JobManager
    {
        private Dictionary<Guid, JobContext> _jobs = new Dictionary<Guid, JobContext>();

        public Guid CreateJob(string script, string args, bool debug)
        {
            Guid g = Guid.NewGuid();
            lock (_jobs)
                _jobs.Add(g, new JobContext(script, args, debug));
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