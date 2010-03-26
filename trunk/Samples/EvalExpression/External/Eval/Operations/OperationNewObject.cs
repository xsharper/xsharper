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
    /// Create a new object
    [Serializable]
    public class OperationNewObject : IOperation
    {
        readonly string _typeName;
        readonly int _paramCount;
        readonly bool _isInitializerProvided;

        /// <summary>
        /// Create a new object and push it to stack
        /// </summary>
        /// <param name="typeName">Type name to create. May contain ? and/or suffix. For example, int[] or int? or int or System.Int32</param>
        /// <param name="paramCount">Number of constructor parameters</param>
        /// <param name="isInitProvided">true if stack also contain an initializer (arrays only on the moment)</param>
        public OperationNewObject(string typeName, int paramCount, bool isInitProvided)
        {
            _typeName = typeName;
            _paramCount = paramCount;
            _isInitializerProvided = isInitProvided;
        }

        /// Returns number of entries added to stack by the operation. 
        public int StackBalance { get { return 1-(_paramCount); } }

        /// Evaluate the operation against stack
        public void Eval(IEvaluationContext context, Stack<object> stack)
        {
            string typeName = _typeName;
            object arrInit = null;
            int pc = _paramCount;
            object[] o=null;
            int c = 0;
            if (_isInitializerProvided)
            {
                arrInit = stack.Pop();
                if (!(arrInit is System.Collections.IEnumerable))
                    throw new InvalidOperationException("Invalid array initializer");

                bool empty = (typeName == "[]");
                if ((arrInit is Array) && !empty)
                {
                    c = ((Array)arrInit).Length;
                }
                else
                {
                    Type tc = null;
                    List<object> rr=new List<object>();
                    foreach (var array in (arrInit as System.Collections.IEnumerable))
                    {
                        rr.Add(array);
                        if (empty)
                            tc = Utils.CommonBase(array, tc);
                    }
                    c = rr.Count;
                    arrInit = rr;
                    if (tc != null)
                        typeName = tc.FullName+"[]";
                }
                pc--;
                if (pc==0)
                    o = new object[1] {c};
                
            }
            if (pc > 0)
                o = OperationHelper.PopArray(stack, pc);

            object ret=null;
            if (string.IsNullOrEmpty(typeName))
                ret = arrInit;
            else
            {
                var tn = OperationHelper.ResolveType(context, typeName);
                if (tn == null)
                    throw new TypeLoadException("Failed to resolve type '" + _typeName + "'");
                if (!tn.IsArray && _isInitializerProvided)
                    throw new InvalidOperationException("Initializers can only be provided to arrays");
                if (!Utils.TryCreateInstance(tn, o, context.AccessPrivate, out ret))
                    throw new InvalidOperationException("Failed to create object of type '" + _typeName + "'");
            }
            if (_isInitializerProvided && !string.IsNullOrEmpty(typeName))
            {
                if (o!=null && o.Length != 0 && (int) o[0] != c)
                    throw new InvalidOperationException("Array initializer length does not match array size");
                var par = (Array) ret;
                var elType = par.GetType().GetElementType();
                c = 0;
                foreach (var array in (System.Collections.IEnumerable)arrInit)
                    par.SetValue(Utils.To(elType, array), c++);
            }
            stack.Push(ret);
        }

        /// Returns a <see cref="T:System.String"/> that represents the current object.
        public override string ToString()
        {
            return "newObj(" + _typeName + ", " + _paramCount + " args..." + (_isInitializerProvided ? ",initializer" : string.Empty)+" )";
        }
    }
}