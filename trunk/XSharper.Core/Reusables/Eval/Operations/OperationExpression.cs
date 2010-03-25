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
    ///<summary>Evaluate expression, as a sequence of operations</summary>
    [Serializable]
    public class OperationExpression : IOperation
    {
        private readonly IOperation[] _data;

        /// Returns number of entries added to stack by the operation. 
        public int StackBalance
        {
            get
            {
                int r = 0;
                foreach (var element in _data)
                {
                    r += element.StackBalance;
                }
                return r;
            }
        }

        /// Constructor
        public OperationExpression(params IOperation[] e)
        {
            _data = e;
        }


        /// Evaluate the operation against stack
        public void Eval(IEvaluationContext context, Stack<object> stack)
        {
            foreach (var element in _data)
                element.Eval(context, stack);
        }

        /// Return string representation of the expression
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("expr {");
            for (int i = 0; i < _data.Length; ++i)
            {
                if (i != 0)
                    sb.Append(", ");
                sb.Append(_data[i].ToString());
            }
            sb.Append("}");
            return sb.ToString();

        }
    }
}