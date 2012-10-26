#region -- Copyrights --
// ***********************************************************************
//  This file is a part of XSharper (http://xsharper.com)
// 
//  Copyright (C) 2006 - 2010, Alexei Shamov, DeltaX Inc.
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//  
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
//  
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
// ************************************************************************
#endregion
using System;
using System.Diagnostics;
using System.Text;
using System.Web;
using System.Xml;
using System.IO;

namespace RunScript
{
    [Serializable]
    public class RunScriptContext : JobContext
    {
        private readonly StringBuilder _currentHtml = new StringBuilder();
        private readonly XSharper.Core.ScriptContext _context = new XSharper.Core.ScriptContext();
        private XSharper.Core.OutputType? _outputType;
        private bool _dirtyString = false;
        private readonly string _scriptXml;
        private readonly string _scriptFilename;
        private readonly string _args;
        private readonly bool _debug;

        public RunScriptContext(string filename, string xml, string args, bool debug)
        {
            _scriptFilename = filename;
            _scriptXml = xml;
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

        public override void Stop()
        {
            _context.Abort();
            base.Stop();
        }

        #region -- Private stuff --
        protected override void Execute()
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

                XSharper.Core.Script script;
                if (!string.IsNullOrEmpty(_scriptFilename))
                    script= _context.LoadScript(_scriptFilename,false);
                else
                    script= _context.LoadScript(XmlReader.Create(new StringReader(_scriptXml)),"temp.xsh");
                _context.Initialize(script);
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

        void output(object sender, XSharper.Core.OutputEventArgs eventArgs)
        {
            lock (_currentHtml)
            {
                string text = eventArgs.Text;
                XSharper.Core.OutputType otype = eventArgs.OutputType;
                if (_outputType.HasValue && otype != _outputType && _dirtyString)
                {
                    if (!(otype == XSharper.Core.OutputType.Bold && _outputType == XSharper.Core.OutputType.Out) &&
                        !(otype == XSharper.Core.OutputType.Out && _outputType == XSharper.Core.OutputType.Bold))
                    {
                        _currentHtml.Append("<br />");
                    }
                }
                _dirtyString = false;
                if (!string.IsNullOrEmpty(text))
                {
                    if (otype != XSharper.Core.OutputType.Out)
                        _currentHtml.Append("<span class='" + otype + "'>");

                    char c = text[text.Length - 1];
                    _dirtyString=!(c == '\n' || c == '\r') ;
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