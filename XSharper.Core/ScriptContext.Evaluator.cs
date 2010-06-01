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
using System.Runtime.CompilerServices;

namespace XSharper.Core
{
    public partial class ScriptContext : IEvaluationContext
    {

        /// <summary>
        /// Override this method to provide extra handling of specified method or property call. Default implementation throws an exception
        /// </summary>
        /// <param name="name">name of the property or method</param>
        /// <param name="args">arguments</param>
        /// <returns>call return value</returns>
        public virtual object CallExternal(string name, object[] args)
        {
            return Call(name, args);
        }

        /// <summary>
        /// Find type by name
        /// </summary>
        /// <param name="typeName">Type to find by name</param>
        /// <returns>found type, or null if not found</returns>
        public virtual Type FindType(string typeName)
        {
            Type t = Utils.FindType(typeName);
            if (t==null)
            {
                t = Compiler.TryResolveTypeNameWithUsing(typeName, _typeManager.AllAssemblies);
            }
            return t;
        }
        
        /// Returns token characters, in addition to ASCII letters and digits. 
        protected virtual string TokenSpecialCharacters
        {
            get
            {
                return "@_%~";
            }
        }


        private static EnvironmentVariableTarget getTarget(ref string variableName)
        {
            variableName = variableName.TrimStart('%').TrimEnd('%').Trim();
            EnvironmentVariableTarget target = EnvironmentVariableTarget.Process;
            if (variableName.StartsWith("user:", StringComparison.OrdinalIgnoreCase))
            {
                target = EnvironmentVariableTarget.User;
                variableName = variableName.Substring(5);
            }
            if (variableName.StartsWith("machine:", StringComparison.OrdinalIgnoreCase))
            {
                target = EnvironmentVariableTarget.Machine;
                variableName = variableName.Substring(8);
            }
            return target;
        }

        /// <summary>
        /// Determines whether the specified variable is calculated.
        /// </summary>
        /// <param name="key">Name of the variable.</param>
        /// <returns>
        /// 	<c>true</c> if the specified variable name is calculated, i.e. not stored in the collection; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsCalculated(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                char ch = key[0];
                if (ch=='%' || ch=='~')
                    return true;
            }
            if (!string.IsNullOrEmpty(key) && (key[0] == '=' || key.IndexOf('|') != -1 || char.IsDigit(key[0])))
                return true;
            return false;
        }

        /// Override this method to resolve calculated variables to object
        public virtual bool TryGetExternal(string s, out object o)
        {
            switch (s.ToLowerInvariant())
            {
                case "context":
                case "c":
                    o = this;
                    return true;
                case "null":
                    o = null;
                    return true;
                case "true":
                    o = true;
                    return true;
                case "false":
                    o = false;
                    return true;
            }
            o = null;
            return false;
        }


        /// Set variable
        public override void Set(string key, object value, bool append)
        {
            while (key.StartsWith("+", StringComparison.Ordinal))
            {
                append = true;
                key = key.Substring(1);
            }

            if (!string.IsNullOrEmpty(key))
            {
                switch (key[0])
                {
                    case '%':
                        EnvironmentVariableTarget target = getTarget(ref key);
                        string v = Utils.To<string>(value);
                        if (append)
                            v = Environment.GetEnvironmentVariable(key, target) + v;
                        Environment.SetEnvironmentVariable(key, v, target);
                        return;
                }                
            }
            if (IsCalculated(key))
                throw new InvalidOperationException(string.Format("Variable '{0}' is calculated and cannot be modified", key));

            base.Set(key, value, append);
        }

        /// Remove variable
        public override bool Remove(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                switch (key[0])
                {
                    case '%':
                        Set(key, null);
                        return true;
                }
            }
            return base.Remove(key);
        }


        /// <summary>
        /// Try to get variable value
        /// </summary>
        /// <param name="key">Variable name</param>
        /// <param name="value">Where to store found variable value</param>
        /// <returns>true if variable is set</returns>
        public override bool TryGetValue(string key, out object value)
        {
            if (!IsCalculated(key))
                return base.TryGetValue(key, out value);
            using (var sr = new ParsingReader(key))
            {
                sr.SkipWhiteSpace();
                value = sr.ReadNumber();
                if (value != null)
                {
                    sr.SkipWhiteSpace();
                    sr.ReadAndThrowIfNot(-1);
                    return true;
                }
            }

            
            if (!string.IsNullOrEmpty(key))
            {
                switch (key[0])
                {
                    case '%':
                        EnvironmentVariableTarget target = getTarget(ref key);
                        value = Environment.GetEnvironmentVariable(key, target);
                        return value!=null;
                    case '~':
                        value = Find(key.Substring(1), true);
                        return true;
                }
            }
            value = EvalMulti(key);
            return true;
        }

        /// Read expression from string, evaluate it and return the value
        public object Eval(string expression)
        {
            if (expression == null)
                return null;
            
            IOperation ex = _exprCache[expression];
            if (ex == null)
            {
                using (var sr = new ParsingReader(new StringReader(expression)))
                {
                    ex = new Parser(TokenSpecialCharacters).Parse(sr);
                    sr.SkipWhiteSpace();
                    sr.ReadAndThrowIfNot(-1);
                    _exprCache[expression] = ex;
                }
            }
            if (ex == null)
                return null;
            return Eval(ex);
        }

        /// Read expression from stream, evaluate it and return the value
        public object Eval(ParsingReader expressionReader)
        {
            return Eval(new Parser(TokenSpecialCharacters).Parse(expressionReader));
        }

        /// Read multi-expression (like ${a|b|=3+5}) from the string, evaluate it and return the value
        public object EvalMulti(string multiExpression)
        {
            if (multiExpression==null)
                return null;
            IOperation o = _exprCache[multiExpression];
            if (o == null)
            {
                using (var sr = new ParsingReader(new StringReader(multiExpression)))
                {
                    o = new Parser(TokenSpecialCharacters).ParseMulti(sr);
                    sr.SkipWhiteSpace();
                    sr.ReadAndThrowIfNot(-1);
                    _exprCache[multiExpression] = o;
                }
            }
            return Eval(o);
        }

        /// Read multi-expression (like ${a|b|=3+5}) from the stream and evaluate it
        public object EvalMulti(ParsingReader expressionReader)
        {
            return Eval(new Parser(TokenSpecialCharacters).ParseMulti(expressionReader));
        }

        /// Evaluate expression
        public object Eval(IOperation operation)
        {
            if (operation == null)
                return null;

            var stack = new Stack<object>();
            operation.Eval(this, stack);
            return stack.Pop();
        }
        /// Get list of no-name objects or type to try methods that start with .
        public IEnumerable<TypeObjectPair> GetNonameObjects()
        {
            yield return new TypeObjectPair(GetType(), this);
            yield return new TypeObjectPair(GetType(), null);
            yield return new TypeObjectPair(typeof(Utils), null);
            yield return new TypeObjectPair(typeof(Dump), null);
        }

        /// Returns true if private members may be accessed
        public virtual bool AccessPrivate
        {
            get; set;
        }
    }
}