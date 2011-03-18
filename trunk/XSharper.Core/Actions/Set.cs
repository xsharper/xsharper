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
using System.Text.RegularExpressions;

namespace XSharper.Core
{
    /// Base abstract class for actions that set variables or action properties
    public abstract class SetBase : ScriptActionBase
    {
        private object _value;

        /// Variable name
        public string Name { get; set; }

        /// Value to set
        [XsAttribute(""),XsAttribute("value")]
        public object Value
        {
            get { return _value; }
            set { _value = value;
                IsValueSet = true; }
        }

        /// Returns true if value has been set, false otherwise.
        protected bool IsValueSet { get; private set; }

        /// <summary>
        /// Called when XML Reader reads an attribute or a text field
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="attribute">Attribute name, or an empty string for element text</param>
        /// <param name="value">Attribute value</param>
        /// <param name="previouslyProcessed">List of previously processed attributes, to detect duplicate attributes. May be null if duplicate attributes are allowed.</param>
        /// <returns> true, if the attribute if correctly processed and false otherwise </returns>
        protected override bool ProcessAttribute(IXsContext context, string attribute, string value, IDictionary<string, bool> previouslyProcessed)
        {
            if (!base.ProcessAttribute(context, attribute, value, previouslyProcessed))
            {
                if (Name != null)
                    throw new ParsingException("Only a single variable may be set by set action.");
                Name = attribute;
                Value = value;
                if (previouslyProcessed!=null && !string.IsNullOrEmpty(attribute))
                    previouslyProcessed.Add(attribute,true);
            }
            return true;
        }
    }

    /// Set variable value
    [XsType("set", ScriptActionBase.XSharperNamespace, AnyAttribute = true)]
    [Description("Set variable value")]
    public class Set : SetBase
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public Set()
        {
            
        }
        
        /// Constructor accepting variable name and value to set
        public Set(string name, string value)
        {
            Name = name;
            Value = value;
        }

        /// Constructor accepting variable name, value to set and transformation rules for variable and value
        public Set(string name, string value, TransformRules tr) : this(name,value)
        {
            Transform = tr;
        }

        /// Execute action
        public override object Execute()
        {
            string name = Context.TransformStr(Name, Transform);
            object v= Context.Transform(Value, Transform);
            if (name!=null)
                if (!IsValueSet)
                    Context.Remove(name);
                else
                    Context[name] = v;
            return null;
        }
    }
}