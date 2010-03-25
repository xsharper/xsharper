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
    /// Helpful utilities used by different operations
    public static class OperationHelper
    {
        /// <summary>
        /// Get top objects from stack in reverse order (i.e. the topmost object on stack is the last array item)
        /// </summary>
        /// <param name="stack">Stack</param>
        /// <param name="count">Number of objects to pop</param>
        /// <returns>Retrieved objects</returns>
        public static object[] PopArray(Stack<object> stack, int count)
        {
            object[] p = new object[count];
            for (int i = 0; i < count; ++i)
                p[p.Length - 1 - i] = stack.Pop();
            return p;
        }

        /// <summary>
        /// Try to resolve typename to type. This is a helper function on top of <see cref="IEvaluationContext.FindType"/> that parses [] and ? suffix
        /// </summary>
        /// <param name="context"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Type ResolveType(IEvaluationContext context, string name)
        {
            bool nullable = false;
            bool array = false;
            if (name.EndsWith("[]", StringComparison.Ordinal))
            {
                array = true;
                name = name.Substring(0, name.Length - 2);
            }
            if (name.EndsWith("?", StringComparison.Ordinal))
            {
                nullable = true;
                name = name.Substring(0, name.Length - 1);
            }
            Type t = context.FindType(name);
            if (t != null)
            {
                if (nullable)
                    t = typeof(Nullable<>).MakeGenericType(t);
                if (array)
                    t = t.MakeArrayType();
            }
            return t;
        }
    }
}