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
    /// A class that wraps any thrown exception with a script location where it occurred
    /// </summary>
    [Serializable]
    public class ScriptExceptionWithStackTrace : Exception
    {
        /// Constructor
        protected ScriptExceptionWithStackTrace(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
            
            Details = info.GetString("_details");
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context">Current script context</param>
        /// <param name="inner">Inner exception</param>
        public ScriptExceptionWithStackTrace(ScriptContext context, Exception inner)
            : base(generateMessage(context.CallStack),inner)
        {
            
            Details = "  "+context.CallStack.Format("\n  ", "", false, false);
        }

        /// <summary>
        /// Exception details
        /// </summary>
        public string Details { get; private set; }

        private static string generateMessage(CallStack stack)
        {
            if (stack.Count == 0)
                return "before script execution";
            return "at " + stack.Peek().ToString();
        }

        /// <summary>
        /// When overridden in a derived class, sets the <see cref="T:System.Runtime.Serialization.SerializationInfo"/> with information about the exception.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown. 
        ///                 </param><param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination. 
        ///                 </param><exception cref="T:System.ArgumentNullException">The <paramref name="info"/> parameter is a null reference (Nothing in Visual Basic). 
        ///                 </exception>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("_details", Details);
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        /// <returns>
        /// The error message that explains the reason for the exception, or an empty string("").
        /// </returns>
        public override string Message
        {
            get
            {
                return InnerException.Message ;
            }
        }

        /// Returns a <see cref="T:System.String"/> that represents the current object.
        public override string ToString()
        {
            return InnerException.ToString() + Environment.NewLine + "At script location: " + Environment.NewLine + Details;
        }
    }
}