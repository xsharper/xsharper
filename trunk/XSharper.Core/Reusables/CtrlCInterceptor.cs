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
using System.Threading;
using System.IO;

namespace XSharper.Core
{
    
    /// Control C interceptor that aborts the thread, or exit the whole program in the specified period of time
    public class CtrlCInterceptor : IDisposable
    {
        private System.Threading.Timer _timer;
        private ConsoleCtrl _ctl;
        private bool _exitIfAbort;
        private bool _ignoreCtrlC;
        private readonly ManualResetEvent _canExit = new ManualResetEvent(false);

        /// Event fired on abort
        public event EventHandler Abort;
        
        /// Where to write ^C when event happens
        public TextWriter Output { get; set; }

        /// Time between the keyboard event and threadAbort exception
        public TimeSpan AbortDelay { get;set;}

        /// Time between the threadAbort exception and the forceful program termination
        public TimeSpan ExitDelay { get; set; }

        /// true, if Ctrl+C is to be ignored (Ctrl+Break should work)
        public bool IgnoreCtrlC
        {
            get { return _ignoreCtrlC; }
            set
            { 
                _ignoreCtrlC = value;
            }
        }

        /// Which thread should be aborted
        public Thread ThreadToAbort
        {
            get; set;
        }

        /// Constructor
        public CtrlCInterceptor(TimeSpan abortDelay, TimeSpan exitDelay, bool ignoreCtrlC) : this()
        {
            AbortDelay = abortDelay;
            ExitDelay = exitDelay;
            IgnoreCtrlC = ignoreCtrlC;
        }

        /// Constructor
        public CtrlCInterceptor()
        {
            _ctl = new ConsoleCtrl();
            _ctl.ControlEvent += ctrlEvent;
            ThreadToAbort = Thread.CurrentThread;
            AbortDelay = TimeSpan.FromSeconds(5);
            ExitDelay = TimeSpan.FromSeconds(10);
            Output = Console.Error;
        }
        
        /// Destructor
        ~CtrlCInterceptor()
        {
            Dispose(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                KillAbortTimer();
                if (_ctl!=null)
                    _ctl.Dispose();
            }
            _ctl = null;
        }

        private void ctrlEvent(object sender, ConsoleCtrlEventArgs consoleEvent)
        {
            consoleEvent.CallDefaultHandler = false;
            if (IgnoreCtrlC && consoleEvent.ConsoleEvent == ConsoleEvent.CtrlC)
                return;

                
            if (_timer == null)
            {
                
                _timer = new System.Threading.Timer(abortFunc, null, (long)AbortDelay.TotalMilliseconds, Timeout.Infinite);
                try
                {
                    var ou = Output;
                    if (ou != null)
                        ou.WriteLine("^C");

                    var ab = Abort;
                    ab.Invoke(this, new EventArgs());

                    _canExit.WaitOne();
                    Thread.Sleep(500);
                }
                catch
                {
                }
            }

        }

        private void abortFunc(object o)
        {
            if (!_exitIfAbort)
            {
                var t = ThreadToAbort;
                if (t!=null)
                    t.Abort();
                if (_timer != null)
                {
                    // 
                    _timer.Change((long)ExitDelay.TotalMilliseconds, Timeout.Infinite);
                    _exitIfAbort = true;
                }
            }
            else
            {
                _canExit.Set();
                Environment.Exit(-1);
            }
        }

        /// Cancel abort timer
        public void KillAbortTimer()
        {
            if (_timer != null)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                _timer.Dispose();
            }
            _timer = null;
            _exitIfAbort = false;
            _canExit.Set();
        }
    }

    
}