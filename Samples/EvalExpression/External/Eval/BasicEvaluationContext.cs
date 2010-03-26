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
using System.IO;

namespace XSharper.Core
{
    /// Basic implementation of the IEvaluationContext
    public class BasicEvaluationContext : IEvaluationContext
    {
        private readonly Parser _parser=new Parser();
        private readonly Dictionary<string, object> _variables = new Dictionary<string, object>();
        private readonly Dictionary<string, object> _objects = new Dictionary<string, object>();
        private readonly List<string> _namespaces = new List<string> { "System", "System.Text", "System.Collections", "System.Text.RegularExpressions" };

        /// Return current parser
        public Parser Parser    {   get { return _parser; } }

        /// Return dictionary of variables (that are referenced with $ prefix, for example $a+$b )
        public Dictionary<string, object> Variables {   get { return _variables; }  }

        /// Return dictionary of objects (that are referenced without $ prefix, for example a+b.Length )
        public Dictionary<string, object> Objects   {   get { return _objects; }   }

        /// Return dictionary of namespaces that are searched when trying to resolve type.
        /// By default System, System.Text, System.Text.RegularExpressions and System.Collections are resolved.
        public List<string> Namespaces  {   get { return _namespaces; } }


        #region ** IEvaluationContext members **

        /// Get external variable (variable specified as $xxx. For example, $x+$y )
        public virtual bool TryGetValue(string name, out object value) { return Variables.TryGetValue(name, out value); }
        
        /// Try to get external object. This is different from variable and not prepended by $. For example, c.WriteLine('hello')
        public virtual bool TryGetExternal(string name, out object value) { return Objects.TryGetValue(name, out value); }
        
        /// Returns true if private members may be accessed
        public virtual bool AccessPrivate { get { return false; } }
        
        /// Call external method
        public virtual object CallExternal(string name, object[] parameters) { throw new NotImplementedException("Function " + name + " is not available"); }

        /// Get list of no-name objects or type to try methods that start with .
        public virtual IEnumerable<TypeObjectPair> GetNonameObjects() { return null; }

        /// Find external type. Returns null if type not found
        public virtual Type FindType(string name)
        {
            Type t=Utils.FindType(name);
            if (t!=null)
                return t;
            foreach(var ns in Namespaces)
            {
                t = Utils.FindType(ns + "." + name);
                if (t!=null)
                    return t;
            }
            return null;
        }
        #endregion

        
        /// Constructor
        public BasicEvaluationContext()
        {
            _objects.Add("null",null);
            _objects.Add("true", true);
            _objects.Add("false", false);
        }

        /// Constructor
        public BasicEvaluationContext(IEqualityComparer<string> cmp) : this()
        {
            _variables = new Dictionary<string, object>(cmp);
            _objects = new Dictionary<string, object>(cmp);
        }

        /// Constructor
        public BasicEvaluationContext(Parser parser) : this()
        {
            _parser = parser;
        }
        
        /// Evaluate string and return the result of the evaluation
        public object Eval(string st)
        {
            return Eval<object>(st);
        }

        /// Evaluate string and return the result of the evaluation, casted to the type T
        public T Eval<T>(string st)
        {
            using (var sr = new ParsingReader(new StringReader(st)))
            {
                var o = Eval<T>(sr);
                sr.SkipWhiteSpace();
                sr.ReadAndThrowIfNot(-1);
                return o;
            }
        }

        /// Parse stream until the end of the expression, evaluate it, and return result of the evaluation
        public object Eval(ParsingReader sr)
        {
            return Eval<object>(sr);
        }

        /// Parse stream until the end of the expression, evaluate it, and return result of the evaluation converted to type T
        public T Eval<T>(ParsingReader sr)
        {
            var p = Parser.Parse(sr);
            if (p == null)
                return Utils.To<T>(null);
            return Eval<T>(p);
        }

        /// Evaluate the specified operation and return result of the evaluation
        public T Eval<T>(IOperation op)
        {
            Stack<object> st = new Stack<object>();
            op.Eval(this,st);
            return Utils.To<T>(st.Pop());
        }

        /// Evaluate the specified operation and return result of the evaluation converted to type T
        public object Eval(IOperation op)
        {
            return Eval<object>(op);
        }
        
    }
}