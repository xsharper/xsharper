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
using System.ComponentModel;

namespace XSharper.Core
{
    /// <summary>
    /// Stop execution of the current subroutine or script, and return the provided value to the caller.
    /// </summary>
    [XsType("return", ScriptActionBase.XSharperNamespace)]
    [Description("Stop execution of the current subroutine or script, and return the provided value to the caller.")]
    public class Return : ValueBase
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public Return()
        {
            
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="value">value for Value property of the created object</param>
        /// <param name="rules">value for Transform property of the created object</param>
        public Return(object value, TransformRules rules)
        {
            Value = value;
            Transform = rules;
        }

        /// Execute action
        public override object Execute()
        {
            return new ReturnValue(Context.Transform(Value, Transform));
        }
    }

    /// Break out of a loop
    [XsType("break", ScriptActionBase.XSharperNamespace)]
    [Description("Break out of a loop")]
    public class Break : ScriptActionBase
    {
        /// Execute action
        public override object Execute()
        {
            return ReturnValue.Break;
        }
    }
}