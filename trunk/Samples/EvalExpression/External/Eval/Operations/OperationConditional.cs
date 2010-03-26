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
using System.Text;

namespace XSharper.Core.Operations
{
    ///<summary>Conditional operator. Get the top value from stack, convert it to bool, and execute one of the expressions</summary>
    [Serializable]
    public class OperationConditional : IOperation
    {
        readonly IOperation _ifTrue;
        readonly IOperation _ifFalse;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ifTrue">Expression to execute if the condition is true</param>
        /// <param name="ifFalse">Expression to execute if the condition is false</param>
        public OperationConditional(IOperation ifTrue, IOperation ifFalse)
        {
            _ifTrue = ifTrue;
            _ifFalse = ifFalse;
        }

        /// Returns number of entries added to stack by the operation. 
        public int StackBalance
        {
            get
            {
                return 1-Math.Max(_ifTrue.StackBalance,_ifFalse.StackBalance);
            }
        }

        /// Evaluate the operation against stack
        public void Eval(IEvaluationContext context, Stack<object> stack)
        {
            var cond = Utils.To<bool>(stack.Pop());
            if (cond)
                _ifTrue.Eval(context,stack);
            else
                _ifFalse.Eval(context, stack);
        }

        /// Returns a string representation of the current object
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("conditional(");
            sb.Append(_ifTrue);
            sb.Append(", ");
            sb.Append(_ifFalse);
            sb.Append(")");
            return sb.ToString();
        }

    }

}