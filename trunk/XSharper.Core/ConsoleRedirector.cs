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
using System.IO;

namespace XSharper.Core
{
    /// <summary>
    /// Redirect console in/out/error to ScriptContext, with automatic release when Dispose is called
    /// </summary>
    public class ConsoleRedirector : IDisposable
    {
        TextWriter _oldOut = Console.Out;
        TextWriter _oldError = Console.Error;
        TextReader _oldIn = Console.In;

        /// <summary>
        /// Redirect console in/out/error streams
        /// </summary>
        /// <param name="toContext">context where to redirect</param>
        public ConsoleRedirector(ScriptContext toContext)
        {
            Console.SetOut(toContext.Out);
            Console.SetError(toContext.Error);
            Console.SetIn(toContext.In);
        }


        /// Destructor
        ~ConsoleRedirector()
        {
            Dispose(false);
        }

        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// Restore original redirection
        public void Restore()
        {
            if (_oldOut != null)
            {
                Console.SetOut(_oldOut);
                Console.SetError(_oldError);
                Console.SetIn(_oldIn);
            }
            _oldOut = null;
            _oldError = null;
            _oldIn = null;
        }

        /// <summary> Releases the managed resources </summary>
        /// <param name="disposing">true to release managed resources; false to release only unmanaged resources (does nothing). </param>
        protected void Dispose(bool disposing)
        {
            if (disposing)
                Restore();
            _oldOut = null;
            _oldError = null;
            _oldIn = null;
        }
    }
}