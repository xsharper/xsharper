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
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace XSharper.Core
{
    /// <summary>
    /// Delay script execution 
    /// </summary>
    [XsType("sleep", ScriptActionBase.XSharperNamespace)]
    [Description("Delay script execution")]
    public class Sleep : ScriptActionBase
    {
        /// Default constructor
        public Sleep()
        {
            
        }

        /// Sleep for the specific period
        public Sleep(string timespan)
        {
            Timeout = timespan;
        }

        /// Sleep for the specific period
        public Sleep(TimeSpan timespan)
        {
            Timeout = timespan.ToString();
        }

        /// Sleep for the specific period, in milliseconds
        public Sleep(long milliseconds)
        {
            Timeout = milliseconds.ToString();
        }

        /// Timeout, can be specified as a number of milliseconds, or as timespan (00:00:00)
        [XsRequired]
        public string Timeout { get; set;}

        /// Execute action
        public override object Execute()
        {
            TimeSpan? ts = Utils.ToTimeSpan(Context.TransformStr(Timeout, Transform));

            if (!ts.HasValue || ts.Value.TotalMilliseconds==0)
                return null;

            VerboseMessage("Sleeping for {0}", ts);

            
            Stopwatch w=Stopwatch.StartNew();
            var totalMs = ts.Value.TotalMilliseconds;
            using (var s=new WaitableTimer(ts))
            {
                while (!s.WaitHandle.WaitOne(200, false))
                    Context.OnProgress((int)(w.ElapsedMilliseconds / totalMs * 100));
            }
 
            return null;
        }
    }
}