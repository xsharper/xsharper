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
using System.Security.Permissions;

namespace XSharper.Core
{
    /// <summary>
    /// This exception is thrown if script terminates by Die action. This exception is automatically rethrown at the end of all Catch Blocks.
    /// </summary>
    [Serializable]
    public class ScriptTerminateException : ScriptException
    {
        /// <summary>
        /// Exit code. Default -1
        /// </summary>
        public int ExitCode { get; private set; }

        /// <summary>
        /// Default constructor 
        /// </summary>
        public ScriptTerminateException() : this(-1)
        {
        }

        /// Constructor
        public ScriptTerminateException(int exitCode) : this(exitCode,null)
        {
        }

        /// Constructor
        public ScriptTerminateException(int exitCode, Exception inner)
            : base(string.Format("Script terminated with exit code {0}", exitCode),inner)
        {
            ExitCode = exitCode;
        }

        /// Constructor
        public ScriptTerminateException(string message, int exitCode, Exception inner)
            : base(message, inner)
        {
            ExitCode = exitCode;
        }

        /// Constructor for serialization
        protected ScriptTerminateException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
            ExitCode = info.GetInt32("_exitCode");
        }

        /// <summary>
        /// When overridden in a derived class, sets the <see cref="T:System.Runtime.Serialization.SerializationInfo"/> with information about the exception.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown. 
        ///                 </param><param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination. 
        ///                 </param><exception cref="T:System.ArgumentNullException">The <paramref name="info"/> parameter is a null reference (Nothing in Visual Basic). 
        ///                 </exception>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("_exitCode", ExitCode);
            base.GetObjectData(info, context);
        }
    }
}