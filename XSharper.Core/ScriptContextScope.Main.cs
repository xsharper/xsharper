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

namespace XSharper.Core
{
    /// Script context scope, contains a script context associated with the current thread
    public class ScriptContextScope : IDisposable
    {
        private ScriptContext _old,_set;
        private bool _disposed = false;

        /// Return current script context
        public static ScriptContext Current
        {
            get { return s_current??DefaultContext; }
        }

        /// Default global ScriptContext to be used when there is no active ScriptContextScope.
        public static ScriptContext DefaultContext { get; set; }

        [ThreadStatic]
        static ScriptContext s_current;

        /// Constructor that saves the current context and sets the provided context as current
        public ScriptContextScope(ScriptContext ctx)
        {
            _old = s_current;
            _set = ctx;
            s_current = ctx;
        }

        /// Destructor
        ~ScriptContextScope()
        {
            Dispose();
        }

        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        public void Dispose()
        {
            if (!_disposed)
            {
                if (ReferenceEquals(_set,s_current))
                    s_current = _old;
                _disposed = true;
                _set = null;
                _old = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}