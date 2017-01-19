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
using System.IO;

namespace XSharper.Core
{
    /// <summary>
    /// Set of variables, which can be accessed by name or by index
    /// </summary>
    public partial class VarsWithExpand : Vars, IEvaluationContext
    {
        private PrecompiledCache _exprCache;

        /// <summary>
        /// Constructor
        /// </summary>
        public VarsWithExpand()
        {
        }

        public PrecompiledCache Cache
        {
            get { return _exprCache; }
            set { _exprCache = value; }
        }

        /// <summary>
        /// Constructor that creates a deep copy of the provided enumeration
        /// </summary>
        public VarsWithExpand(Vars setOfVariables) : base(setOfVariables)
        {
            
        }
        /// <summary>
        /// Constructor that creates a deep copy of the provided enumeration
        /// </summary>
        public VarsWithExpand(IEnumerable<Var> setOfVariables) : base(setOfVariables)
        {
        }
        /// <summary>
        /// Constructor that creates a deep copy of the provided enumeration
        /// </summary>
        public VarsWithExpand(IEnumerable<KeyValuePair<string, object>> setOfVariables) : base(setOfVariables)
        {
        }

        

        #region -- Variable expansion --
        ///<summary>Transform variable</summary>
        ///<param name="source">Original variable value</param>
        ///<param name="rules">Transformation rules (<see cref="TransformRules"/></param>
        ///<returns>Transformed object</returns>
        public object Transform(object source, TransformRules rules)
        {
            object s = source;
            if (source != null && rules != TransformRules.None)
            {
                Type t = source.GetType();
                if (t == typeof(Nullable<>))
                    t = Nullable.GetUnderlyingType(t);
                if (t.IsPrimitive || t == typeof(Guid) || t == typeof(decimal))
                    rules &= ~(TransformRules.ExpandMask | TransformRules.TrimMask);

                if ((rules & TransformRules.ExpandAfterTrim) == TransformRules.ExpandAfterTrim)
                {
                    if (s != null)
                        s = Utils.TransformStr(Utils.To<string>(s), rules & TransformRules.TrimMask);
                    rules = (rules & ~TransformRules.ExpandAfterTrim & ~TransformRules.TrimMask) | TransformRules.Expand;
                }
                if ((rules & TransformRules.Expand) != 0)
                    s = expandVars(rules, Utils.To<string>(s));
                if ((rules & TransformRules.ExpandReplaceOnly) == TransformRules.ExpandReplaceOnly)
                    rules &= ~TransformRules.ReplaceMask;
                if ((rules & TransformRules.ExpandTrimOnly) == TransformRules.ExpandTrimOnly)
                    rules &= ~TransformRules.TrimMask;
                if ((rules & ~TransformRules.ExpandMask) != TransformRules.None)
                {
                    if (s != null)
                        s = Utils.TransformStr(Utils.To<string>(s), rules & ~TransformRules.Expand);
                }
            }
            return s;
        }
        ///<summary>Transform string to another string</summary>
        ///<param name="source">Original variable value</param>
        ///<param name="rules">Transformation rules (<see cref="TransformRules"/></param>
        ///<returns>Transformed string</returns>
        public string TransformStr(string source, TransformRules rules)
        {
            return Utils.To<string>(Transform(source, rules));
        }

        /// Expand ${} variables 
        public object Expand(object arguments)
        {
            return Transform(arguments, TransformRules.Expand);
        }

        /// Expand ${} variables 
        public string ExpandStr(object arguments)
        {
            return Utils.To<string>(Expand(arguments));
        }

        /// <summary>
        /// Verify that transformation is correct
        /// </summary>
        /// <param name="text">Text to verify</param>
        /// <param name="expand">Rules</param>
        public void AssertGoodTransform(string text, TransformRules expand)
        {
            if (String.IsNullOrEmpty(text))
                return;

            if ((expand & TransformRules.Expand) == TransformRules.Expand && text.Contains("${"))
            {
                if (!text.Contains("}"))
                    throw new ParsingException("Closing } not found in '" + text + "'");
            }
            if ((expand & TransformRules.ExpandDual) == TransformRules.ExpandDual && text.Contains("${{"))
            {
                if (!text.Contains("}}"))
                    throw new ParsingException("Closing }} not found in '" + text + "'");
            }
            if ((expand & TransformRules.ExpandDualSquare) == TransformRules.ExpandDualSquare && text.Contains("[["))
            {
                if (!text.Contains("]]"))
                    throw new ParsingException("Closing ]] not found in '" + text + "'");
            }
            if ((expand & TransformRules.ExpandSquare) == TransformRules.ExpandDualSquare && text.Contains("["))
            {
                if (!text.Contains("]"))
                    throw new ParsingException("Closing ] not found in '" + text + "'");
            }
        }

        /// <summary>
        /// Check if text is expression that needs to be calculated if transformed according to the expansion rules
        /// </summary>
        /// <param name="text">Text to verify</param>
        /// <param name="expand">Rules</param>
        /// <returns>true if text contains expressions</returns>
        public bool ContainsExpressions(string text, TransformRules expand)
        {
            if (String.IsNullOrEmpty(text))
                return false;
            if ((expand & TransformRules.Expand) == TransformRules.Expand)
                return text.Contains("${");
            if ((expand & TransformRules.ExpandDual) == TransformRules.ExpandDual)
                return text.Contains("${{");
            if ((expand & TransformRules.ExpandDualSquare) == TransformRules.ExpandDualSquare)
                return text.Contains("[[");
            if ((expand & TransformRules.ExpandSquare) == TransformRules.ExpandSquare)
                return text.Contains("[");
            return false;
        }
        private object expandVars(TransformRules rules, string s)
        {
            string begin;
            string end;
            if ((rules & TransformRules.ExpandDual) == TransformRules.ExpandDual)
            {
                begin = "${{";
                end = "}}";
            }
            else if ((rules & TransformRules.ExpandDualSquare) == TransformRules.ExpandDualSquare)
            {
                begin = "[[";
                end = "]]";
            }
            else if ((rules & TransformRules.ExpandSquare) == TransformRules.ExpandSquare)
            {
                begin = "[";
                end = "]";
            }
            else if ((rules & TransformRules.Expand) == TransformRules.Expand)
            {
                begin = "${";
                end = "}";
            }
            else
                return s;

            if (s.IndexOf(begin, StringComparison.Ordinal) != -1)
            {
                StringBuilder sbNew = new StringBuilder();
                using (var sr = new ParsingReader(new StringReader(s)))
                {
                    int ptr = 0;
                    bool first = true;
                    while (!sr.IsEOF)
                    {
                        char ch = (char)sr.Read();
                        if (ch != begin[ptr])
                        {
                            sbNew.Append(begin, 0, ptr);
                            sbNew.Append(ch);
                            ptr = 0;
                            first = false;
                            continue;
                        }
                        ptr++;
                        if (ptr < begin.Length)
                            continue;
                        if (sr.Peek() == '{' || sr.Peek() == '[')
                        {
                            sbNew.Append(begin);
                            ptr = 0;
                            first = false;
                            continue;
                        }
                        // 
                        object sv = EvalMulti(sr);
                        sv = ((rules & TransformRules.ExpandTrimOnly) == TransformRules.ExpandTrimOnly)
                                 ? Utils.TransformStr(Utils.To<string>(sv), rules & TransformRules.TrimMask)
                                 : sv;
                        sv = ((rules & TransformRules.ExpandReplaceOnly) == TransformRules.ExpandReplaceOnly)
                                 ? Utils.TransformStr(Utils.To<string>(sv), rules & TransformRules.ReplaceMask)
                                 : sv;

                        // Now read the trailing stuff
                        sr.SkipWhiteSpace();
                        for (ptr = 0; ptr < end.Length; ++ptr)
                            sr.ReadAndThrowIfNot(end[ptr]);
                        if (sr.IsEOF && first)
                            return sv;
                        ptr = 0;
                        first = false;
                        sbNew.Append(Utils.To<string>(sv));
                    }
                    for (int i = 0; i < ptr; ++i)
                        sbNew.Append(begin[i]);
                }


                return sbNew.ToString();
            }
            return s;
        }
        #endregion

        /// Returns token characters, in addition to ASCII letters and digits. 

        protected virtual string TokenSpecialCharacters
        {
            get
            {
                return "@_";
            }
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
            if (!string.IsNullOrEmpty(key) && (key[0] == '=' || key.IndexOf('|') != -1 || char.IsDigit(key[0])))
                return true;
            return false;
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
            
            value = EvalMulti(key);
            return true;
        }

        /// Read expression from string, evaluate it and return the value
        public object Eval(string expression)
        {
            if (expression == null)
                return null;

            if (_exprCache == null)
                _exprCache = new PrecompiledCache(128);

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
            if (multiExpression == null)
                return null;
            if (_exprCache == null)
                _exprCache = new PrecompiledCache(128);
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

        /// Returns true if private members may be accessed
        public virtual bool AccessPrivate
        {
            get; set;
        }

        /// Get list of no-name objects or type to try methods that start with .
        public virtual IEnumerable<TypeObjectPair> GetNonameObjects()
        {
            yield break;
        }

        /// <summary>
        /// Find type by name
        /// </summary>
        /// <param name="typeName">Type to find by name</param>
        /// <returns>found type, or null if not found</returns>
        public virtual Type FindType(string typeName)
        {
            return Utils.FindType(typeName, false);
        }

        /// <summary>
        /// Override this method to provide extra handling of specified method or property call. Default implementation throws an exception
        /// </summary>
        /// <param name="name">name of the property or method</param>
        /// <param name="args">arguments</param>
        /// <returns>call return value</returns>
        public virtual object CallExternal(string name, object[] args)
        {
            throw new ParsingException("A subroutine with id=" + name + " not found");
        }

        public virtual bool TryGetExternal(string s, out object o)
        {
            switch (s.ToLowerInvariant())
            {
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
    }
}