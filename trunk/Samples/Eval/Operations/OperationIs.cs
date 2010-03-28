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
    ///<summary>Cast the object on top of the stack to the given type, then push it back</summary>
    [Serializable]
    public class OperationIs : IOperation
    {
        readonly string _typeName;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="typeName">Type name. May contain ? and/or suffix. For example, int[] or int? or int or System.Int32</param>
        public OperationIs(string typeName)
        {
            _typeName = typeName;
        }

        /// Returns an number of entries added to stack by the operation. 0 in this case
        public int StackBalance { get { return 0; } }

        /// Evaluate the operation against stack
        public void Eval(IEvaluationContext context, Stack<object> stack)
        {
            var p = stack.Pop();
            Type t = OperationHelper.ResolveType(context, _typeName);
            if (t == null)
                throw new TypeLoadException("Failed to resolve type '" + _typeName + "'");
            if (p==null)
                stack.Push(false);
            else
                stack.Push(t.IsAssignableFrom(p.GetType()));
        }

        /// Returns a <see cref="T:System.String"/> that represents the current object.
        public override string ToString()
        {
            return "is(" + _typeName + ")";
        }
    }
}