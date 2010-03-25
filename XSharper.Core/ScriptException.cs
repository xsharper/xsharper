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
using System.Runtime.Serialization;

namespace XSharper.Core
{
    /// <summary>
    /// Base exception class for exceptions thrown by the scripting engine.
    /// </summary>
    [Serializable]
    public class ScriptException : Exception
    {
        /// Default constructor
        public ScriptException()
        {
        }

        /// constructor with message
        public ScriptException(string message)
            : base(message)
        {
        }

        /// constructor with message and inner exception
        public ScriptException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// constructor for serialization
        protected ScriptException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// Exception thrown by script runtime
    /// </summary>
    [Serializable]
    public class ScriptRuntimeException : ScriptException
    {
        /// Default constructor
        public ScriptRuntimeException()
        {
        }

        /// Constructor with message
        public ScriptRuntimeException(string message)
            : base(message)
        {
        }

        /// Constructor with message and inner exception
        public ScriptRuntimeException(string message, Exception inner)
            : base(message, inner)
        {
        }


        /// Serialization constructor
        protected ScriptRuntimeException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// Exception thrown by user script
    /// </summary>
    [Serializable]
    public class ScriptUserException : ScriptException
    {
        /// Default constructor
        public ScriptUserException()
        {
        }

        /// Constructor with message
        public ScriptUserException(string message)
            : base(message)
        {
        }

        /// Constructor with message and inner exception
        public ScriptUserException(string message, Exception inner)
            : base(message, inner)
        {
        }


        /// Serialization constructor
        protected ScriptUserException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }

    
}   