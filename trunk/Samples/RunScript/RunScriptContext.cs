using System;
using System.Diagnostics;
using System.Text;
using System.Web;

namespace RunScript
{
    [Serializable]
    public class RunScriptContext : JobContext
    {
        private readonly StringBuilder _currentHtml = new StringBuilder();
        private readonly XSharper.Core.ScriptContext _context = new XSharper.Core.ScriptContext();
        private XSharper.Core.OutputType? _outputType;
        private readonly string _scriptText;
        private readonly string _args;
        private readonly bool _debug;

        public RunScriptContext(string scriptText, string args, bool debug)
        {
            _scriptText = scriptText;
            _args = args;
            _debug = debug;
            Start();
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

        public void Stop()
        {
            _context.Abort();
        }
        
        #region -- Private stuff --
        protected override void Execute()
        {
            using (var xs = new XSharper.Core.ScriptContextScope(_context))
            {
                Stopwatch sw = Stopwatch.StartNew();
                try
                {
                    _context.Clear();
                    _context.Output = output;
                    if (_debug)
                        _context.MinOutputType = XSharper.Core.OutputType.Nul;
                    else
                        _context.MinOutputType = XSharper.Core.OutputType.Info;

                    var script = _context.CreateNewScript("temp.xsh");
                    script.Load(_scriptText);
                    var ret = _context.ExecuteScript(script, XSharper.Core.Utils.SplitArgs(_args));
                    lock (_currentHtml)
                    {
                        _currentHtml.Append("<hr />");
                        _outputType = null;
                    }
                    _context.Info.WriteLine("Execution completed in {0}. Exit code={1}", sw.Elapsed, ret ?? 0);
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

                    _currentHtml.Append(HttpUtility.HtmlEncode(text).Replace("\n", "<br/>").Replace(" ", "&nbsp;"));

                    if (otype != XSharper.Core.OutputType.Out)
                        _currentHtml.Append("</span>");
                }
                _outputType = otype;
            }
        }
        #endregion
    }
}