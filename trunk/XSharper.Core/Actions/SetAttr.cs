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
using System.ComponentModel;
using System.Reflection;

namespace XSharper.Core
{
    /// <summary>
    /// Set action attribute
    /// </summary>
    [XsType("setattr", ScriptActionBase.XSharperNamespace, AnyAttribute = true)]
    [Description("Set action attribute")]
    public class SetAttr: Set
    {
        /// <summary>
        /// Action identifier
        /// </summary>
        public string ActionId { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public SetAttr()
        {
            
        }

        /// Constructor
        public SetAttr(string actionId, string name, object value)
        {
            ActionId = actionId;
            Name = name;
            Value = value;
        }

        /// Constructor
        public SetAttr(string actionId, string name, object value, TransformRules tr) : this(actionId,name,value)
        {
            Transform = tr;
        }

        /// Execute action
        public override object Execute()
        {
            string actId = Context.TransformStr(ActionId, Transform);
            string name = Context.TransformStr(Name, Transform);
            object v = Context.Transform(Value, Transform);
            Context.Find(actId, true).SetAttr(name,v);
            return null;
        }
    }
}