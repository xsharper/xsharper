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
using System.Collections;
using System.Collections.Generic;

namespace XSharper.Core
{
    
    /// Pair of type and object
    [Serializable]
    public class TypeObjectPair
    {
        /// Object type
        public Type Type { get; private set; }

        /// Optional object (may be null)
        public object Object { get; private set; }


        ///Constructor
        public TypeObjectPair(Type type, object o)
        {
            Type = type;
            Object = o;
        }
    }

    
    /// Evaluation context, providing interface to the external resources
    public interface IEvaluationContext
    {
        /// Get external variable (variable specified as $xxx. For example, $x+$y )
        bool    TryGetValue(string name, out object value);

        /// Find external type. Returns null if type not found
        Type    FindType(string name);

        /// Call external method
        object  CallExternal(string name, object[] parameters);

        /// Try to get external object. This is different from variable and not prepended by $. For example, c.WriteLine('hello')
        bool    TryGetExternal(string name, out object value);

        /// Get list of no-name objects or type to try methods that start with .
        IEnumerable<TypeObjectPair> GetNonameObjects();

        /// Access private methods
        bool AccessPrivate { get;  }

        /// Returns true if COM interop is allowed
        bool AllowComInterop { get; }

        /// Returns true if dump operator ## is allowed
        bool AllowDumpOperator { get; }

        /// Make a call to a method or property
        bool TryGetOrCall(object obj, Type objType, bool isProperty, string propertyOrMethod, Array args, out object retVal);
    }

    /// Compiled script expression
    public interface IOperation
    {
        /// <summary>
        /// Evaluate the operation against stack
        /// </summary>
        /// <param name="context">Evaluation context</param>
        /// <param name="stack">Stack</param>
        void Eval(IEvaluationContext context, Stack<object> stack);

        /// <summary>
        /// Returns an number of entries added to stack by the operation. For example, result for Push would be 1, and for Pop would be -1.
        /// </summary>
        int  StackBalance { get; }
    }


    
}
