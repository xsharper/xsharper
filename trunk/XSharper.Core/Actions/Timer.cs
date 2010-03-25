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
using System.Globalization;

namespace XSharper.Core
{
    /// Timer action output format
    public enum TimerFormat
    {
        /// Output as TimeSpan
        [Description("Output as TimeSpan")]
        TimeSpan,

        /// Output to rounded long number of milliseconds
        [Description("Output to rounded long number of milliseconds")]
        TimeSpanRounded,

        /// Output start and stop local times as string 'TIME1 ... TIME2' 
        [Description("Output start and stop local times as string 'TIME1 ... TIME2' ")]
        TimeRange,

        /// Output as integer number of milliseconds
        [Description("Output as integer number of milliseconds")]
        Milliseconds,

        /// Output as integer number of milliseconds
        [Description("Output as integer number of milliseconds")]
        Ms=Milliseconds,

        /// Output as integer number of seconds
        [Description("Output as integer number of seconds")]
        Seconds,

        /// Output as number of ticks
        [Description("Output as number of ticks")]
        Ticks,

        /// Output as string "Elapsed time: XX:XX:XX.XXX"
        [Description("Output as string \"Elapsed time: XX:XX:XX.XXX\"")]
        Elapsed
    }

    /// Execute block and recprd execution time
    [XsType("timer", ScriptActionBase.XSharperNamespace)]
    [Description("Execute block and record execution time")]
    public class Timer : Block
    {
        /// Where to output execution time, default null
        [Description("Where to output execution time")]
        public string OutTo { get; set;}

        /// How to format execution time. Default TimeSpan
        [Description("How to format execution time")]
        public TimerFormat Format { get; set; }

        /// Default constructor
        public Timer()
        {
            Format = TimerFormat.TimeSpan;
        }

        /// Constructor acceptiong format and output destination
        public Timer(TimerFormat type, string outTo) : this()
        {
            Format = type;
            OutTo = outTo;
        }

        /// Execute action
        public override object Execute()
        {
            DateTime dt1 = DateTime.Now;
            Stopwatch sw = Stopwatch.StartNew();

            object ret = null;
            try
            {
                ret = base.Execute();
            }
            finally
            {
            
                sw.Stop();
                DateTime dt2 = DateTime.Now;
                object v;
                switch (Format)
                {
                    default:
                        v = sw.Elapsed;
                        break;
                    case TimerFormat.TimeSpanRounded:
                        v = TimeSpan.FromMilliseconds((long)sw.Elapsed.TotalMilliseconds);
                        break;
                    case TimerFormat.TimeRange:
                        v = string.Format(CultureInfo.CurrentCulture, "{0} ... {1}", dt1, dt2);
                        break;
                    case TimerFormat.Elapsed:
                        v = string.Format(CultureInfo.CurrentCulture, "Elapsed time: {0}" + Environment.NewLine, sw.Elapsed);
                        break;
                    case TimerFormat.Milliseconds:
                        v = ((long)sw.Elapsed.TotalMilliseconds);
                        break;
                    case TimerFormat.Seconds:
                        v = ((long)sw.Elapsed.TotalSeconds);
                        break;
                    case TimerFormat.Ticks:
                        v = ((long)sw.Elapsed.Ticks);
                        break;
                }
            
                Context.OutTo(Context.TransformStr(OutTo, Transform), v);
            }
            return ret;
                
        }
    }
}