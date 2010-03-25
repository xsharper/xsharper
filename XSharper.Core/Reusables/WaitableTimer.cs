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

namespace XSharper.Core
{
    /// <summary>
    /// Utility timer class that can be used in WaitHandle.WaitAny/WaitAll scenarios
    /// </summary>
    public class WaitableTimer : IDisposable
    {
        private EventWaitHandle _handle;
        private readonly System.Threading.Timer _timer;

        /// <summary>
        /// Create a waitable timer with the specified timeout
        /// </summary>
        /// <param name="timeout">Time to wait before signaling</param>
        public WaitableTimer(TimeSpan? timeout) 
        {
            _handle = new ManualResetEvent(false);
            if (timeout!=null)
                _timer = new System.Threading.Timer(callback, null, (long)timeout.Value.TotalMilliseconds, System.Threading.Timeout.Infinite);
        }

        

        /// <summary>
        /// Get the underlying WaitHandle
        /// </summary>
        public WaitHandle WaitHandle
        {
            get { return _handle; }
        }

        private void callback(object state)
        {
            try
            {
                if (_handle != null)
                    _handle.Set();
            }
            catch
            {
            }
        }


        /// Destructor
        ~WaitableTimer()
        {
            Dispose(false);
        }

        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// Dispose managed resources
        protected virtual void Dispose(bool disposing)
        {
            if (_timer != null)
            {
                if (disposing)
                {
                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
                    WaitHandle wh = new ManualResetEvent(false);
                    _timer.Dispose(wh);
                    wh.WaitOne();
                }
            }
            if (_handle != null)
            {
                _handle.Close();
                _handle = null;
            }
        }


    }
}