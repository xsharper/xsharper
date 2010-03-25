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
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
using System;
using System.Collections;

namespace XSharper.Core
{
    /// <summary>
    /// Execute a block for every item in the sequence
    /// </summary>
    [XsType("foreach", ScriptActionBase.XSharperNamespace)]
    [Description("Execute a block for every item in the sequence")]
    public class ForEach : Block
    {
        /// Rowset to enumerate
        [Description("Rowset to enumerate")]
        public string RowsetId { get; set; }

        /// Variable prefix
        [Description("Variable prefix")]
        public string Name { get; set; }

        /// Value to set
        [XsAttribute("in")]
        [XsAttribute("value", Deprecated = true)]
        [Description("Value to be enumerated")]
        public object In
        {
            get;
            set;
        }

        /// Maximum number of loops. null = infinite
        [Description("Maximum number of loops. null = infinite")]
        [XsAttribute("maxCount"), XsAttribute("maxLoops", Deprecated = true), XsAttribute("max", Deprecated = true), XsAttribute("count", Deprecated = true)]
        public int? MaxCount { get; set; }

        object baseExecute()
        {
            return base.Execute();
        }
        
        /// Execute action
        public override object Execute()
        {
            string id=Context.TransformStr(RowsetId, Transform);
            string pref = Context.TransformStr(Name, Transform);

            int cnt = 0;
            if (!string.IsNullOrEmpty(id))
            {
                RowSet rs = Context.Find<RowSet>(id,true);
                foreach (Vars sv in rs.GetData())
                {
                    if (MaxCount != null && cnt< MaxCount)
                        break;
                    cnt++;
                    object r = Context.ExecuteWithVars(baseExecute, sv, pref);
                    if (r != null)
                    {
                        if (ReturnValue.IsBreak(r))
                            return null;
                        return r;
                    }
                }
            }


            if (In != null)
            {
                object v = Context.Transform(In, Transform);
                if (!(v is IEnumerable) || v.GetType()==typeof(string))
                    v = new object[] {v};
                
                Vars sv=new Vars();
                foreach (object o in (IEnumerable)v)
                {
                    Context.CheckAbort();

                    if (MaxCount != null && cnt < MaxCount)
                        break;
                    cnt++;

                    sv[string.Empty] = o;
                    object r = Context.ExecuteWithVars(baseExecute, sv, pref);
                    if (ReturnValue.IsBreak(r))
                        return null;
                    if (r != null)
                        return r;
                }   
            }
            return null;
        }

        /// <summary>
        /// Called when XML Reader reads an attribute or a text field
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="attribute">Attribute name, or an empty string for element text</param>
        /// <param name="value">Attribute value</param>
        /// <param name="previouslyProcessed">List of previously processed attributes, to detect duplicate attributes. May be null if duplicate attributes are allowed.</param>
        /// <returns>true, if the attribute if correctly processed and false otherwise</returns>
        protected override bool ProcessAttribute(IXsContext context, string attribute, string value, IDictionary<string, bool> previouslyProcessed)
        {
            if (!base.ProcessAttribute(context, attribute, value, previouslyProcessed))
            {
                if (Name != null)
                    throw new ParsingException("Only a single variable may be set.");
                Name = attribute;
                In = value;
                if (previouslyProcessed != null && !string.IsNullOrEmpty(attribute))
                    previouslyProcessed.Add(attribute, true);
            }
            return true;
        }
    }
}