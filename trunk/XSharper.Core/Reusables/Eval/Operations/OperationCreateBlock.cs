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
using System.Collections.Generic;

namespace XSharper.Core.Operations
{
    ///<summary>Replace top N objects on top of the stack with a single collection object</summary>
    [Serializable]
    public class OperationCreateBlock : IOperation
    {
        readonly int _paramCount;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="paramCount">Number of objects to read from stack</param>
        public OperationCreateBlock(int paramCount)
        {
            _paramCount=paramCount;
        }

        /// Returns number of entries added to stack by the operation. 
        public int StackBalance
        {
            get { return 1-_paramCount; }
        }

        /// Evaluate the operation against stack
        public void Eval(IEvaluationContext context, Stack<object> stack)
        {
            var o = OperationHelper.PopArray(stack, _paramCount);

            // Find a general object type
            Type parent = null;
            foreach (var o1 in o)
                parent = Utils.CommonBase(o1, parent);
            
            if (parent == null || parent == typeof(object))
                stack.Push(o);
            else
            {
                Array a = (Array)Activator.CreateInstance(parent.MakeArrayType(), o.Length);
                for (int i = 0; i < a.Length; ++i)
                    a.SetValue(o[i], i);
                stack.Push(a);
            }
        }

        /// Return string representation of the expression
        public override string ToString()
        {
            return "block(" + _paramCount + ")";
        }
    }
}