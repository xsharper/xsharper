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
using System.Runtime.InteropServices;

namespace XSharper.Core
{
    static partial class NativeMethods
    {
        /// Handler to be called when a console event occurs.
        public delegate bool ConsoleControlEventHandler(ConsoleEvent consoleEvent);

        [DllImport("kernel32.dll")]
        internal static extern bool SetConsoleCtrlHandler(ConsoleControlEventHandler e, bool add);
    }

    /// <summary>
    /// A console event that occurred.
    /// </summary>
    public enum ConsoleEvent
    {
        /// ControlC
        CtrlC = 0,
        /// CtrlBreak
        CtrlBreak = 1,
        /// Close program
        CtrlClose = 2,
        /// Logoff
        CtrlLogOff = 5,
        /// System shutdown
        CtrlShutdown = 6
    }


    /// Console event handler
    public class ConsoleCtrlEventArgs : EventArgs
    {
        /// Constructor
        public ConsoleCtrlEventArgs(ConsoleEvent consoleEvent ) {ConsoleEvent = consoleEvent;}

        /// Console event that occured
        public ConsoleEvent ConsoleEvent { get; private set;}

        /// true, if default handler should be called
        public bool CallDefaultHandler { get; set; }
    }

    /// <summary>
    /// Console event handling
    /// </summary>
    /// <remarks>
    /// Courtesy of http://www.hanselman.com/blog/MoreTipsFromSairamaCatchingCtrlCFromANETConsoleApplication.aspx
    /// </remarks>
    public class ConsoleCtrl : IDisposable
    {
        private NativeMethods.ConsoleControlEventHandler eventHandler;

        /// Event fired when a console event occurs
        public event EventHandler<ConsoleCtrlEventArgs> ControlEvent;

        

        /// Constructor
        public ConsoleCtrl()
        {
            // save this to a private var so the GC doesn't collect it...
            try
            {
                eventHandler = new NativeMethods.ConsoleControlEventHandler(handler);
                NativeMethods.SetConsoleCtrlHandler(eventHandler, true);
            }
            catch (System.EntryPointNotFoundException)
            {
                eventHandler = null;
            }
        }

        /// Destructor
        ~ConsoleCtrl() { Dispose(false); }

        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// Free managed resources
        protected virtual void Dispose(bool disposing)
        {
            if (eventHandler != null)
            {
                NativeMethods.SetConsoleCtrlHandler(eventHandler, false);
                eventHandler = null;
            }
        }

        private bool handler(ConsoleEvent consoleEvent)
        {
            if (ControlEvent != null)
            {
                var a = new ConsoleCtrlEventArgs(consoleEvent);
                a.CallDefaultHandler = true;
                ControlEvent(this, a);
                return !a.CallDefaultHandler;
            }
            return false;
        }

        
    }
}