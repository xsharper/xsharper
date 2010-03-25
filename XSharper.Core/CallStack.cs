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
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace XSharper.Core
{
    /// Script operation execution phase
    public enum ScriptOperation
    {
        /// Unknown
        [Description("Unknown")]
        None,

        /// Script is loading
        [Description("Script is loading")]
        Loading,

        /// Script is initializing
        [Description("Script is initializing")]
        Initializing,

        /// Script is compiling with C# compiler
        [Description("Script is compiling with C# compiler")]
        Compiling,

        /// About to execute, parsing command line arguments
        [Description("About to execute, parsing command line arguments")]
        ParsingArguments,

        /// Executing
        [Description("Executing")]
        Executing
    }

    /// Call stack element
    public class CallStackItem 
    {
        /// Script action
        public IScriptAction ScriptAction { get; private set; }

        /// Execution phase of this script action
        public ScriptOperation Operation { get; private set; }

        
        /// Constructor
        [DebuggerHidden]
        public CallStackItem(ScriptOperation operation, IScriptAction o)
        {
            Operation = operation;
            ScriptAction = o;
        }

        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        [DebuggerHidden]
        public override string ToString()
        {
            string s = "[" + Operation.ToString() + "] ";
            if (ScriptAction != null)
                s += ScriptAction.ToString();
            else
                s += "<NULL>";
            return s;
        }
    } ;


    /// Call stack
    public class CallStack : Stack<CallStackItem>
    {
        /// Push an action on top of the call stack
        [DebuggerHidden]
        public void Push(ScriptOperation operation, IScriptAction action)
        {
            Push(new CallStackItem(operation,action));
        }

        /// Remove the top action from the call stack
        [DebuggerHidden]
        public new void Pop() {
            base.Pop();
        }

        /// Return topmost script action
        public Script GetCurrentScript()
        {
            // Return first filename from the callstack
            CallStackItem[] cs = ToArray();
            for (int i = 0; i < cs.Length; i++)
            {
                Script s = cs[i].ScriptAction as Script;
                if (s != null)
                    return s;
            }
            return null;
        }

        /// Return bottomless script action
        public Script GetMainScript()
        {
            // Return first filename from the callstack
            CallStackItem[] cs = ToArray();
            for (int i = cs.Length - 1; i >= 0; i--)
            {
                Script s = cs[i].ScriptAction as Script;
                if (s != null)
                    return s;
            }
            return null;
        }

        /// <summary>
        /// Find an action with a specified ID
        /// </summary>
        /// <typeparam name="T">Type of action to find</typeparam>
        /// <param name="id">ID of the action to find</param>
        /// <returns>Found action or null if not found</returns>
        public T FindTree<T>(string id) where T: class,IScriptAction
        {
            // Push all objects
            foreach (CallStackItem item in this)
            {
                if (item.ScriptAction != null)
                {
                    IScriptAction v = ScriptContext.FindTree( item.ScriptAction, delegate(IScriptAction x)
                                                                                     {
                                                                                         if (!(x is T))
                                                                                             return false;
                                                                                         if (id != null)
                                                                                             return string.Compare(id, x.Id, StringComparison.OrdinalIgnoreCase) == 0;
                                                                                         return false;
                                                                                     });
                    if (v != null)
                        return (T)v;
                }
            }
            return null;
        }

        /// <summary>
        /// Look what's at the top of the stack
        /// </summary>
        [DebuggerHidden]
        public new CallStackItem Peek()
        {
            return base.Peek();
        }

        /// <summary>
        /// Find an action for which the specified predicate is true
        /// </summary>
        /// <typeparam name="T">Type of action to find</typeparam>
        /// <param name="predicate">Predicate to execute</param>
        /// <returns>Found action or null if not found</returns>
        public T Find<T>(Predicate<T> predicate) where T : class, IScriptAction
        {
            return Find<T>(predicate, int.MaxValue);
        }

        /// <summary>
        /// Find an action for which the specified predicate is true
        /// </summary>
        /// <typeparam name="T">Type of action to find</typeparam>
        /// <param name="predicate">Predicate to execute</param>
        /// <param name="nMaxDepth">Maximal search depth</param>
        /// <returns>Found action or null if not found</returns>
        public T Find<T>(Predicate<T> predicate, int nMaxDepth) where T : class, IScriptAction
        {
            foreach (CallStackItem item in this)
            {
                T x = item.ScriptAction as T;
                if (x!=null && (predicate == null || predicate(x)))
                    return x;

                if (nMaxDepth-- < 0)
                    return null;
            }
            return null;
        }

        ///<summary>
        /// Format call stack as readable text for dumps
        ///</summary>
        ///<param name="afterEachLine">Insert this text after each line</param>
        ///<param name="afterLastLine">Insert this text after the last line</param>
        ///<param name="reverse">true, if print in reverse order</param>
        ///<param name="skipDuplicateOperations">if true, do not print operation if it did not change</param>
        ///<returns>Formatted call stack</returns>
        public string Format(string afterEachLine, string afterLastLine, bool reverse, bool skipDuplicateOperations)
        {
            StringBuilder sb = new StringBuilder();
            CallStackItem[] a = ToArray();
            if (reverse)
                Array.Reverse(a);
            bool first = true;
            ScriptOperation oldOperation = ScriptOperation.None;
            foreach (CallStackItem item in a)
            {
                sb.Append(first ? string.Empty : afterEachLine);
                if (!skipDuplicateOperations || item.Operation != oldOperation)
                    sb.Append("[" + item.Operation + "]" + " ");
                sb.Append(item.ScriptAction.ToString());
                first = false;
                oldOperation = item.Operation;
            }
            sb.Append(afterLastLine);
            return sb.ToString();
        }

        /// Return formated call stack
        public string StackTrace
        {
            get
            {
                return Format(Environment.NewLine + "\t", string.Empty, false, false);
            }
        }

        /// Return formated call stack as a single line
        public string StackTraceFlat
        {
            get
            {
                return Format(" >> ", string.Empty, true, true);
            }
        }

        /// Get a value below the top stack element
        public IScriptAction GetCaller()
        {
            return ToArray()[1].ScriptAction;
        }
    }
}