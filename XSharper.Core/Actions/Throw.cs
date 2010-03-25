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

namespace XSharper.Core
{
    /// <summary>
    /// Throw exception with the message provided
    /// </summary>
    [XsType("throw", ScriptActionBase.XSharperNamespace)]
    [Description("Throw exception with the message provided")]
    public class Throw : ValueBase
    {
        /// Exception class name to throw. By default ScriptUserException
        public string Class { get; set; }

        /// Execute action
        public override object Execute()
        {
            string cl = Context.TransformStr(Class, Transform);
            Type t = typeof(ScriptUserException);
            if (!string.IsNullOrEmpty(cl))
            {
                t = Context.FindType(cl);
                if (t == null || !t.IsSubclassOf(typeof (Exception)))
                    t = typeof (ScriptUserException);
            }
            object s = GetTransformedValue();
            if (s!=null)
            {
                object x;
                if (Utils.TryCreateInstance(t, new object [] { s, Context.CurrentException},false,out x) && x is Exception )
                    throw (Exception)x;
            }
            if (Context.CurrentException != null)
                Utils.Rethrow(Context.CurrentException);
            throw (Exception)Utils.CreateInstance(t);
        }
    }



}