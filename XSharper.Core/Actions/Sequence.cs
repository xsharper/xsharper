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
using System.ComponentModel;

namespace XSharper.Core
{
    /// <summary>
    /// A sequence of commands to execute
    /// </summary>
    [XsType("sequence", ScriptActionBase.XSharperNamespace)]
    [Description("A sequence of commands to execute")]
    public class Sequence : ScriptActionBase, IEnumerable<IScriptAction>
    {
        /// List of actions comprising the sequence
        [XsElement("", SkipIfEmpty = true, CollectionItemType = typeof(IScriptAction))]
        public List<IScriptAction> Items { get; set; }

        /// Default constructor
        public Sequence()
        {
            Items = new List<IScriptAction>();
        }

        /// Constructor
        public Sequence(params IScriptAction[] data) 
        {
            Items = new List<IScriptAction>(data);
        }

        /// Execute action
        public override object Execute()
        {
            foreach (IScriptAction item in Items)
            {

                object ret = Context.Execute(item);
                if (ret != null)
                    return ret;
            }
            return null;
        }

        /// <summary>
        /// Execute a delegate for all child nodes of the current node
        /// </summary>
        public override bool ForAllChildren(Predicate<IScriptAction> func,bool isFind)
        {
            if (base.ForAllChildren(func,isFind))
                return true;
            foreach (IScriptAction a in Items)
                if (func(a))
                    return true;
            return false;
        }

        /// <summary>
        /// Initialize action
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            foreach (IScriptAction item in Items)
                Context.Initialize(item);
        }

        /// Add action to the list
        public virtual void Add(IScriptAction child)
        {
            Items.Add(child);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<IScriptAction> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Items.GetEnumerator();
        }
    }
}